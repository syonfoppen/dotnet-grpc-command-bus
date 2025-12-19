using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;
using Syon.CommandBus.Grpc.V1;
using System.Text.Json;

namespace Syon.CommandBus.Grpc;

/// <summary>
/// gRPC endpoint that receives command envelopes, resolves the corresponding command type and handler,
/// executes the command, and returns a transport-friendly <see cref="CommandResult"/>.
///
/// This service is the "receiver runtime" entry point. It hides DI and reflection details from callers
/// and exposes a stable RPC contract defined in <c>commandbus.proto</c>.
/// </summary>
public sealed class CommandBusGrpcService : CommandBusPipe.CommandBusPipeBase
{
    private readonly IServiceProvider _sp;
    private readonly CommandTypeRegistry _registry;
    private readonly JsonSerializerOptions _json;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBusGrpcService"/>.
    /// </summary>
    /// <param name="sp">
    /// The application's root service provider. A scope is created per request to resolve scoped dependencies.
    /// </param>
    /// <param name="registry">
    /// Registry used to map wire-level (name, version) pairs to CLR command types.
    /// </param>
    public CommandBusGrpcService(IServiceProvider sp, CommandTypeRegistry registry)
    {
        _sp = sp;
        _registry = registry;

        // Keep JSON configuration consistent between transmitter and receiver.
        _json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    /// <summary>
    /// Executes a command described by the incoming envelope.
    /// </summary>
    /// <param name="request">The wire envelope containing metadata and serialized command payload.</param>
    /// <param name="context">gRPC call context including cancellation and transport metadata.</param>
    /// <returns>
    /// A <see cref="CommandResult"/> indicating whether the command was accepted, succeeded, or failed.
    /// </returns>
    public override async Task<CommandResult> Execute(CommandEnvelope request, ServerCallContext context)
    {
        try
        {
            // Resolve the CLR command type based on the wire-level identity.
            var commandType = _registry.Resolve(request.CommandName, request.Version);

            // Deserialize the payload JSON into the resolved command type.
            var commandObj = (ICommand?)JsonSerializer.Deserialize(request.PayloadJson, commandType, _json);
            if (commandObj is null)
            {
                return new CommandResult
                {
                    CommandId = request.CommandId,
                    Status = CommandResult.Types.Status.Failed,
                    ErrorCode = "INVALID_PAYLOAD",
                    ErrorMessage = "Payload could not be deserialized."
                };
            }

            // Build the command execution context propagated to the handler.
            var ctx = new CommandContext
            {
                CommandId = request.CommandId,
                CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId,
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey
            };

            // Create a DI scope so handlers can depend on scoped services (e.g., DbContext).
            using var scope = _sp.CreateScope();

            // Resolve ICommandHandler<TCommand> for the resolved command type.
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler is null)
            {
                return new CommandResult
                {
                    CommandId = request.CommandId,
                    Status = CommandResult.Types.Status.Failed,
                    ErrorCode = "NO_HANDLER",
                    ErrorMessage = $"No handler registered for {commandType.Name}."
                };
            }

            // Invoke HandleAsync via reflection to keep the dispatch pipeline non-generic.
            var method = handlerType.GetMethod("HandleAsync");
            if (method is null)
                throw new InvalidOperationException("HandleAsync not found.");

            var task = (Task<DispatchResult>?)method.Invoke(
                handler,
                new object[] { commandObj, ctx, context.CancellationToken });

            if (task is null)
                throw new InvalidOperationException("Handler returned null.");

            var result = await task.ConfigureAwait(false);

            // Map the internal DispatchResult to a transport-level CommandResult.
            return new CommandResult
            {
                CommandId = result.CommandId,
                Status = result.Status switch
                {
                    DispatchStatus.Accepted => CommandResult.Types.Status.Accepted,
                    DispatchStatus.Succeeded => CommandResult.Types.Status.Succeeded,
                    _ => CommandResult.Types.Status.Failed
                },
                ErrorCode = result.ErrorCode ?? "",
                ErrorMessage = result.ErrorMessage ?? ""
            };
        }
        catch (Exception ex)
        {
            // Treat unexpected failures as transport-level failures with a stable error code.
            // Expected business failures should be returned by handlers as DispatchResult.Fail(...).
            return new CommandResult
            {
                CommandId = request.CommandId,
                Status = CommandResult.Types.Status.Failed,
                ErrorCode = "UNHANDLED",
                ErrorMessage = ex.Message
            };
        }
    }
}
