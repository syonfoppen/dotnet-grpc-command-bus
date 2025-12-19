namespace Syon.CommandBus.Core;

/// <summary>
/// Generates unique identifiers for commands.
///
/// Command identifiers are created by the command dispatcher and are used
/// to uniquely identify a command execution across process and service boundaries.
/// </summary>
public interface ICommandIdGenerator
{
    /// <summary>
    /// Generates a new unique command identifier.
    /// </summary>
    /// <returns>
    /// A string that uniquely identifies a command execution.
    /// The exact format is implementation-specific.
    /// </returns>
    string NewId();
}
