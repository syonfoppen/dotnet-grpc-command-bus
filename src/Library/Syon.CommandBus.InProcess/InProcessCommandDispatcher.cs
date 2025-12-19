using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;

namespace Syon.CommandBus.InProcess;

/// <summary>
/// In-process implementation of <see cref="ICommandDispatcher"/>.
///
/// This dispatcher resolves and invokes <see cref="ICommandHandler{TCommand}"/> directly using the application's
/// dependency injection container. It is intended for scenarios where the transmitter and receiver run in the same
/// process (same runtime).
///
/// The public API remains identical to remote dispatchers (such as gRPC) to keep application code transport-agnostic.
/// </summary>
public sealed class InProcessCommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _sp;
    private readonly ICommandIdGenerator _ids;

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessCommandDispatcher"/>.
    /// </summary>
    /// <param name="sp">
    /// Root service provider. A new scope is created per dispatch to correctly support scoped dependencies
    /// (for example DbContext or other request-scoped services).
    /// </param>
    /// <param name="ids">
    /// Generator responsible for producing unique command identifiers.
    /// </param>
    public InProcessCommandDispatcher(IServiceProvider sp, ICommandIdGenerator ids)
    {
        _sp = sp;
        _ids = ids;
    }

    /// <summary>
    /// Sends a command for execution in the current process by resolving the corresponding handler and invoking it.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Cancellation token passed to the handler.</param>
    /// <returns>
    /// A <see cref="DispatchResult"/> indicating whether the command succeeded or failed.
    /// </returns>
    public async Task<DispatchResult> SendAsync(ICommand command, CancellationToken ct = default)
    {
        // Generate an ID for this command execution and pass it to the handler via context.
        var commandId = _ids.NewId();
        var ctx = new CommandContext { CommandId = commandId };

        // Create a scope so handlers can use scoped dependencies safely.
        using var scope = _sp.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Resolve ICommandHandler<TCommand> based on the runtime type of the command.
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = scopedProvider.GetService(handlerType);
        if (handler is null)
        {
            return DispatchResult.Fail(
                commandId,
                "NO_HANDLER",
                $"No handler registered for {command.GetType().Name}");
        }

        // Invoke HandleAsync via reflection to keep the dispatcher non-generic.
        // The expected signature is: Task<DispatchResult> HandleAsync(TCommand, CommandContext, CancellationToken)
        var method = handlerType.GetMethod("HandleAsync");
        if (method is null)
        {
            return DispatchResult.Fail(
                commandId,
                "NO_METHOD",
                "HandleAsync not found on handler");
        }

        try
        {
            var task = (Task<DispatchResult>?)method.Invoke(handler, new object[] { command, ctx, ct });
            if (task is null)
            {
                return DispatchResult.Fail(
                    commandId,
                    "INVALID_HANDLER",
                    "Handler returned null");
            }

            return await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Unexpected exceptions are treated as failures with a stable error code.
            // Expected business failures should be returned by the handler as DispatchResult.Fail(...).
            return DispatchResult.Fail(commandId, "UNHANDLED", ex.Message);
        }
    }
}
