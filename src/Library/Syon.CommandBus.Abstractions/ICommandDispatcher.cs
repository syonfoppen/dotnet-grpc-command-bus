namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Dispatches commands to their corresponding handlers.
///
/// The dispatcher abstracts the underlying execution mechanism.
/// A command may be handled in-process, sent over the network,
/// or forwarded to an asynchronous worker, but callers do not
/// need to know or care how this is implemented.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Sends a command for execution.
    ///
    /// The command is validated, routed, and executed by exactly one handler.
    /// The returned <see cref="DispatchResult"/> indicates whether the command
    /// was accepted, successfully executed, or failed.
    /// </summary>
    /// <param name="command">
    /// The command to dispatch. Must represent an intention to change system state.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the dispatch operation.
    /// Cancellation does not guarantee that command execution itself is aborted,
    /// especially when the command is executed asynchronously or remotely.
    /// </param>
    /// <returns>
    /// A <see cref="DispatchResult"/> describing the outcome of the dispatch operation.
    /// </returns>
    Task<DispatchResult> SendAsync(ICommand command, CancellationToken ct = default);
}
