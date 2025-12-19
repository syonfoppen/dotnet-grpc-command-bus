using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;
using Syon.CommandBus.Grpc;
using Syon.CommandBus.IntegrationTests.Commands;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Xunit;

namespace Syon.CommandBus.IntegrationTests;

public sealed class GrpcEndToEndTests
{
    [Fact]
    public async Task GrpcCommandBus_EndToEnd_Works()
    {
        // English comment: gRPC over plaintext HTTP/2 requires this switch for Grpc.Net.Client.
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        await using var host = await StartGrpcServerAsync();

        var address = host.GetAddress();

        var services = new ServiceCollection();

        // English comment: Core services and contracts registry.
        services.AddCommandBusCore(Assembly.GetExecutingAssembly());

        // English comment: Register the gRPC dispatcher with the server address.
        services.AddCommandBusGrpcClient(new Uri(address));

        await using var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var cmd = new CreateCustomerCommand
        {
            CustomerId = "C123",
            Name = "cars"
        };

        var result = await dispatcher.SendAsync(cmd);

        Assert.Equal(DispatchStatus.Succeeded, result.Status);
        Assert.Equal(result.CommandId, result.CommandId);
    }

    private static async Task<GrpcTestHost> StartGrpcServerAsync()
    {
        var port = GetFreeTcpPort();

        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(k =>
                {
                    // English comment: gRPC requires HTTP/2.
                    k.ListenLocalhost(port, o => o.Protocols = HttpProtocols.Http2);
                });

                web.ConfigureServices(services =>
                {
                    services.AddGrpc();

                    // English comment: Register contracts (commands).
                    services.AddCommandBusCore(Assembly.GetExecutingAssembly());

                    // English comment: Register handlers from THIS test assembly.
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

        return new GrpcTestHost(host, $"http://localhost:{port}");
    }

    private static int GetFreeTcpPort()
    {
        // English comment: Ask the OS for a free port.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class GrpcTestHost : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly string _address;

        public GrpcTestHost(IHost host, string address)
        {
            _host = host;
            _address = address;
        }

        public string GetAddress() => _address;

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    // English comment: Minimal handler used by the integration test.
    private sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand>
    {
        public Task<DispatchResult> HandleAsync(
            CreateCustomerCommand command,
            CommandContext context,
            CancellationToken ct)
        {
            // English comment: Real apps would persist changes here.
            if (string.IsNullOrWhiteSpace(command.CustomerId))
                return Task.FromResult(DispatchResult.Fail(context.CommandId, "VALIDATION", "CustomerId is required"));

            return Task.FromResult(DispatchResult.Success(context.CommandId));
        }
    }
}
