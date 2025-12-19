namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Specifies the stable, transport-level identity of a command.
///
/// This attribute is used by the command bus to map a command type
/// to a wire-level name and version when commands are dispatched
/// across process or service boundaries.
///
/// The name and version together form the public contract of a command.
/// They must remain stable to preserve backward compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CommandNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandNameAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The stable, human-readable name of the command.
    /// This value is used on the wire and must not change once published.
    /// </param>
    /// <param name="version">
    /// The version of the command contract.
    /// Increment this value when making a breaking change to the command payload
    /// or semantics. Defaults to version 1.
    /// </param>
    public CommandNameAttribute(string name, int version = 1)
    {
        Name = name;
        Version = version;
    }

    /// <summary>
    /// Gets the stable wire-level name of the command.
    ///
    /// This name is used by the dispatcher and receiver to resolve
    /// the correct command type and handler across runtimes.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the command contract.
    ///
    /// The version allows multiple iterations of the same command
    /// to coexist, enabling backward-compatible evolution of
    /// distributed command contracts.
    /// </summary>
    public int Version { get; }
}
