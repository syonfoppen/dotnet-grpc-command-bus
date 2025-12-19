namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Handles the execution of a specific command type.
///
/// A command handler contains the application or domain logic required
/// to process a single command. Each command type must have exactly one
/// corresponding handler.
/// </summary>
/// <typeparam name="TCommand">
/// The type of command this handler can process.
/// </typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Executes the given command.
    ///
    /// Implementations should perform all required validation and
    /// state changes needed to fulfill the intent of the command.
    /// Expected business failures should be expressed using
    /// <see cref="DispatchResult"/> rather than by throwing exceptions.
    /// </summary>
    /// <param name="command">
    /// The command to execute. This represents an immutable intent
    /// to change system state.
    /// </param>
    /// <param name="context">
    /// Execution context containing identifiers for correlation,
    /// tracing, and idempotency.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that may be used to observe cancellation requests.
    /// Implementations should respect this token where feasible.
    /// </param>
    /// <returns>
    /// A <see cref="DispatchResult"/> describing the outcome of the command execution.
    /// </returns>
    Task<DispatchResult> HandleAsync(
        TCommand command,
        CommandContext context,
        CancellationToken ct);
}
