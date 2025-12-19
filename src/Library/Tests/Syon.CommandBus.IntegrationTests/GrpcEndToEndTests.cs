using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;
using Syon.CommandBus.Grpc;
using Syon.CommandBus.Grpc.V1;
using Syon.CommandBus.IntegrationTests.Commands;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Syon.CommandBus.IntegrationTests;

public sealed class GrpcEndToEndTests
{
    [Fact]
    public async Task GrpcCommandBus_HappyFlow_Create_Then_Deactivate()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        await using var server = await StartGrpcServerAsync();

        var services = new ServiceCollection();

        // Register command contracts from shared + test assembly (for test-only commands).
        services.AddCommandBusCore(typeof(GrpcEndToEndTests).Assembly);

        // Use a deterministic idempotency key provider so we can assert propagation.
        services.AddSingleton<IIdempotencyKeyProvider, TestIdempotencyKeyProvider>();

        services.AddCommandBusGrpcClient(new Uri(server.Address));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var create = new CreateCustomerCommand
        {
            CustomerId = "C123",
            Name = "cars"
        };

        var createResult = await dispatcher.SendAsync(create);

        Assert.Equal(DispatchStatus.Succeeded, createResult.Status);

        // Verify that the handler observed the propagated context values.
        var createCtx = TestHandlerProbe.TryDequeueContext();
        Assert.NotNull(createCtx);
        Assert.Equal(createResult.CommandId, createCtx!.CommandId);
        Assert.Equal(createResult.CommandId, createCtx.CorrelationId);
        Assert.Equal("CreateCustomer:C123", createCtx.IdempotencyKey);

        var deactivate = new DeactivateCustomerCommand
        {
            CustomerId = "C123",
            Reason = "Customer requested account closure"
        };

        var deactivateResult = await dispatcher.SendAsync(deactivate);

        Assert.Equal(DispatchStatus.Succeeded, deactivateResult.Status);

        var deactivateCtx = TestHandlerProbe.TryDequeueContext();
        Assert.NotNull(deactivateCtx);
        Assert.Equal(deactivateResult.CommandId, deactivateCtx!.CommandId);
        Assert.Equal(deactivateResult.CommandId, deactivateCtx.CorrelationId);
        Assert.Equal("DeactivateCustomer:C123", deactivateCtx.IdempotencyKey);
    }

    [Fact]
    public async Task GrpcCommandBus_Returns_NO_HANDLER_When_Handler_Not_Registered()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        await using var server = await StartGrpcServerAsync();

        var services = new ServiceCollection();
        services.AddCommandBusCore(typeof(GrpcEndToEndTests).Assembly);

        services.AddSingleton<IIdempotencyKeyProvider, TestIdempotencyKeyProvider>();
        services.AddCommandBusGrpcClient(new Uri(server.Address));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        // This command is registered in the registry, but we do NOT register a handler on the server.
        var cmd = new NoHandlerCommand { Value = "X" };
        var result = await dispatcher.SendAsync(cmd);

        Assert.Equal(DispatchStatus.Failed, result.Status);
        Assert.Equal("NO_HANDLER", result.ErrorCode);
    }

    [Fact]
    public async Task GrpcService_Returns_INVALID_PAYLOAD_When_Json_Is_Not_Deserializable()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        await using var server = await StartGrpcServerAsync();

        using var channel = GrpcChannel.ForAddress(
            server.Address,
            new GrpcChannelOptions
            {
                // Required for plaintext HTTP/2.
                HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true }
            });

        var client = new CommandBusPipe.CommandBusPipeClient(channel);

        var env = new CommandEnvelope
        {
            CommandId = "bad-payload-1",
            CorrelationId = "bad-payload-1",
            IdempotencyKey = "",
            CommandName = "CreateCustomer",
            Version = 1,
            PayloadJson = "{ this is not valid json"
        };

        var reply = await client.ExecuteAsync(env);

        Assert.Equal("bad-payload-1", reply.CommandId);
        Assert.Equal(CommandResult.Types.Status.Failed, reply.Status);
        Assert.Equal("INVALID_PAYLOAD", reply.ErrorCode);
    }

    [Fact]
    public async Task GrpcService_Returns_UNHANDLED_When_CommandName_Is_Unknown()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        await using var server = await StartGrpcServerAsync();

        using var channel = GrpcChannel.ForAddress(server.Address);
        var client = new CommandBusPipe.CommandBusPipeClient(channel);

        var env = new CommandEnvelope
        {
            CommandId = "unknown-1",
            CorrelationId = "unknown-1",
            IdempotencyKey = "",
            CommandName = "DoesNotExist",
            Version = 1,
            PayloadJson = "{}"
        };

        var reply = await client.ExecuteAsync(env);

        Assert.Equal("unknown-1", reply.CommandId);
        Assert.Equal(CommandResult.Types.Status.Failed, reply.Status);
        Assert.Equal("UNKNOWN_COMMAND", reply.ErrorCode);
        Assert.Contains("Unknown command", reply.ErrorMessage);
    }

    private static async Task<GrpcTestHost> StartGrpcServerAsync()
    {
        var port = GetFreeTcpPort();
        var address = $"http://localhost:{port}";

        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(k =>
                {
                    // gRPC requires HTTP/2.
                    k.ListenLocalhost(port, o => o.Protocols = HttpProtocols.Http2);
                });

                web.ConfigureServices(services =>
                {
                    services.AddGrpc();

                    // Register shared contracts + test-only command contracts.
                    services.AddCommandBusCore(typeof(GrpcEndToEndTests).Assembly);

                    // Register handlers from THIS test assembly.
                    services.AddCommandHandlersFromAssembly(typeof(GrpcEndToEndTests).Assembly);
                });

                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<CommandBusGrpcService>();
                    });
                });
            });

        var host = builder.Build();
        await host.StartAsync();

        return new GrpcTestHost(host, address);
    }

    private static int GetFreeTcpPort()
    {
        // Ask the OS for a free port.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class GrpcTestHost : IAsyncDisposable
    {
        private readonly IHost _host;
        public string Address { get; }

        public GrpcTestHost(IHost host, string address)
        {
            _host = host;
            Address = address;
        }

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    // Captures contexts from handlers so the test can assert propagation.
    private static class TestHandlerProbe
    {
        private static readonly ConcurrentQueue<CommandContext> _contexts = new();

        public static void Record(CommandContext ctx) => _contexts.Enqueue(ctx);

        public static CommandContext? TryDequeueContext()
            => _contexts.TryDequeue(out var ctx) ? ctx : null;
    }

    // Deterministic idempotency keys for testing.
    private sealed class TestIdempotencyKeyProvider : IIdempotencyKeyProvider
    {
        public string? GetKey(ICommand command) =>
            command switch
            {
                CreateCustomerCommand c => $"CreateCustomer:{c.CustomerId}",
                DeactivateCustomerCommand c => $"DeactivateCustomer:{c.CustomerId}",
                _ => null
            };
    }

    // Minimal handler used by the integration test (Create).
    private sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand>
    {
        public Task<DispatchResult> HandleAsync(CreateCustomerCommand command, CommandContext context, CancellationToken ct)
        {
            TestHandlerProbe.Record(context);

            if (string.IsNullOrWhiteSpace(command.CustomerId))
                return Task.FromResult(DispatchResult.Fail(context.CommandId, "VALIDATION", "CustomerId is required"));

            return Task.FromResult(DispatchResult.Success(context.CommandId));
        }
    }

    // Minimal handler used by the integration test (Deactivate).
    private sealed class DeactivateCustomerHandler : ICommandHandler<DeactivateCustomerCommand>
    {
        public Task<DispatchResult> HandleAsync(DeactivateCustomerCommand command, CommandContext context, CancellationToken ct)
        {
            TestHandlerProbe.Record(context);

            if (string.IsNullOrWhiteSpace(command.CustomerId))
                return Task.FromResult(DispatchResult.Fail(context.CommandId, "VALIDATION", "CustomerId is required"));

            return Task.FromResult(DispatchResult.Success(context.CommandId));
        }
    }

    // Test-only command contract that is registered but has no server handler.
    [CommandName("NoHandler", version: 1)]
    private sealed class NoHandlerCommand : ICommand
    {
        public string Value { get; set; } = "";
    }
}
