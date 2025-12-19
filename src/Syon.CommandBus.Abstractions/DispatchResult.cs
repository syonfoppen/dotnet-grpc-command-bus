namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Represents the outcome of dispatching and executing a command.
///
/// This result is returned by the command dispatcher and command handlers
/// to indicate whether a command was accepted, successfully executed,
/// or failed with a known error.
///
/// DispatchResult is intentionally simple and transport-agnostic so it can
/// be safely mapped across process or service boundaries.
/// </summary>
public sealed class DispatchResult
{
    /// <summary>
    /// Gets the unique identifier of the command this result applies to.
    ///
    /// This value matches the <see cref="CommandContext.CommandId"/> generated
    /// by the dispatcher and can be used for correlation and diagnostics.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the high-level execution status of the command.
    /// </summary>
    public required DispatchStatus Status { get; init; }

    /// <summary>
    /// Gets an optional machine-readable error code.
    ///
    /// This value is intended for programmatic handling of failures
    /// such as retries, user feedback mapping, or conditional logic.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets an optional human-readable error message.
    ///
    /// This message is intended for logging or diagnostic purposes
    /// and should not be relied upon for control flow.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a result indicating that the command was accepted
    /// for processing but may not have completed yet.
    /// </summary>
    /// <param name="commandId">The identifier of the accepted command.</param>
    public static DispatchResult Accepted(string commandId)
        => new() { CommandId = commandId, Status = DispatchStatus.Accepted };

    /// <summary>
    /// Creates a result indicating that the command was successfully executed.
    /// </summary>
    /// <param name="commandId">The identifier of the completed command.</param>
    public static DispatchResult Success(string commandId)
        => new() { CommandId = commandId, Status = DispatchStatus.Succeeded };

    /// <summary>
    /// Creates a result indicating that the command failed.
    /// </summary>
    /// <param name="commandId">The identifier of the failed command.</param>
    /// <param name="errorCode">A stable, machine-readable error code.</param>
    /// <param name="errorMessage">A human-readable description of the failure.</param>
    public static DispatchResult Fail(string commandId, string errorCode, string errorMessage)
        => new()
        {
            CommandId = commandId,
            Status = DispatchStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
