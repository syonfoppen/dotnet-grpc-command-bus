using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;
using Syon.CommandBus.Grpc.V1;
using System.Text.Json;

namespace Syon.CommandBus.Grpc;

/// <summary>
/// gRPC-based implementation of <see cref="ICommandDispatcher"/>.
///
/// This dispatcher serializes a command into a <see cref="CommandEnvelope"/>, sends it to a remote
/// receiver runtime over gRPC, and maps the transport response back to a <see cref="DispatchResult"/>.
///
/// The goal is to make remote command execution look like in-process dispatch to the caller,
/// while still returning an explicit result that can represent success, acceptance, or failure.
/// </summary>
public sealed class GrpcCommandDispatcher : ICommandDispatcher
{
    private readonly CommandBusPipe.CommandBusPipeClient _client;
    private readonly CommandTypeRegistry _registry;
    private readonly ICommandIdGenerator _ids;
    private readonly IIdempotencyKeyProvider _idempotency;
    private readonly JsonSerializerOptions _json;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcCommandDispatcher"/>.
    /// </summary>
    /// <param name="client">
    /// The generated gRPC client used to call the remote command receiver.
    /// </param>
    /// <param name="registry">
    /// Registry used to map CLR command types to wire-level (name, version) pairs.
    /// </param>
    /// <param name="ids">
    /// Generator responsible for producing unique command identifiers.
    /// </param>
    /// <param name="idempotency">
    /// Provider responsible for deriving an idempotency key for a command, or disabling idempotency.
    /// </param>
    public GrpcCommandDispatcher(
        CommandBusPipe.CommandBusPipeClient client,
        CommandTypeRegistry registry,
        ICommandIdGenerator ids,
        IIdempotencyKeyProvider idempotency)
    {
        _client = client;
        _registry = registry;
        _ids = ids;
        _idempotency = idempotency;

        // Use camelCase to match typical JSON naming conventions across services.
        _json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    /// <summary>
    /// Sends a command to the remote receiver runtime for execution.
    /// </summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">
    /// Cancellation token for the network call. Note that cancellation does not guarantee that
    /// the remote side will abort execution once it has started.
    /// </param>
    /// <returns>
    /// A <see cref="DispatchResult"/> indicating whether the command was accepted, succeeded, or failed.
    /// </returns>
    public async Task<DispatchResult> SendAsync(ICommand command, CancellationToken ct = default)
    {
        // Generate a unique identifier for this command submission.
        var commandId = _ids.NewId();

        // Resolve the command's wire identity (name + version) from its CLR type.
        var (name, version) = _registry.GetWire(command.GetType());

        // Build a transport envelope containing metadata and a serialized payload.
        var env = new CommandEnvelope
        {
            CommandId = commandId,

            // For now we reuse commandId as correlationId.
            // In production you typically propagate an upstream correlation id if available.
            CorrelationId = commandId,

            // Empty string means "no idempotency key" for the wire contract.
            IdempotencyKey = _idempotency.GetKey(command) ?? "",

            CommandName = name,
            Version = version,
            PayloadJson = JsonSerializer.Serialize(command, command.GetType(), _json)
        };

        // Execute the remote call.
        var reply = await _client.ExecuteAsync(env, cancellationToken: ct);

        // Map transport response to the library's result type.
        return reply.Status switch
        {
            CommandResult.Types.Status.Succeeded => DispatchResult.Success(reply.CommandId),
            CommandResult.Types.Status.Accepted => DispatchResult.Accepted(reply.CommandId),
            _ => DispatchResult.Fail(
                reply.CommandId,
                reply.ErrorCode ?? "FAILED",
                reply.ErrorMessage ?? "Command failed")
        };
    }
}
