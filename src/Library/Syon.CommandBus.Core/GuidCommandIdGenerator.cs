namespace Syon.CommandBus.Core;

/// <summary>
/// Generates unique command identifiers using GUID version 7.
///
/// GUID v7 is time-ordered, which makes it suitable for use as a command identifier
/// in distributed systems. It preserves uniqueness while also providing better
/// ordering characteristics than random GUIDs (v4), which is beneficial for
/// logging, tracing, and storage in databases or logs.
/// </summary>
public sealed class GuidCommandIdGenerator : ICommandIdGenerator
{
    /// <summary>
    /// Generates a new unique command identifier.
    /// </summary>
    /// <returns>
    /// A lowercase, dashless string representation of a GUID v7.
    /// </returns>
    public string NewId() => Guid.CreateVersion7().ToString("N");
}
