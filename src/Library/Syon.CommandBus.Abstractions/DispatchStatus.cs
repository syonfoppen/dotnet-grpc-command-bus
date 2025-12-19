namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Represents the high-level execution state of a dispatched command.
///
/// This enum intentionally models only the externally observable states
/// of a command, not internal processing steps.
/// </summary>
public enum DispatchStatus
{
    /// <summary>
    /// The command has been accepted for processing.
    ///
    /// This state indicates that the command was validated and enqueued
    /// or handed off to a handler, but execution may still be in progress.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// The command was successfully executed.
    ///
    /// All business logic completed without errors and the intended
    /// side effects were applied.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// The command execution failed.
    ///
    /// The failure is considered final and is accompanied by an error code
    /// and optional error message in the corresponding <see cref="DispatchResult"/>.
    /// </summary>
    Failed = 2
}
