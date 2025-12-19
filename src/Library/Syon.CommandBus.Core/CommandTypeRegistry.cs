using Syon.CommandBus.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Syon.CommandBus.Core;

/// <summary>
/// Maintains a bidirectional mapping between command CLR types and their wire-level identity.
///
/// The command bus uses this registry to:
/// - Convert a command type into a stable (name, version) pair when sending a command remotely.
/// - Resolve an incoming (name, version) pair into a CLR type when receiving a command remotely.
///
/// Commands are discovered by scanning assemblies for types that implement <see cref="ICommand"/>
/// and are annotated with <see cref="CommandNameAttribute"/>.
/// </summary>
public sealed class CommandTypeRegistry
{
    // Maps (wireName, wireVersion) -> CLR Type for receiver-side deserialization.
    private readonly ConcurrentDictionary<(string name, int version), Type> _wireToType = new();

    // Maps CLR Type -> (wireName, wireVersion) for sender-side envelope creation.
    private readonly ConcurrentDictionary<Type, (string name, int version)> _typeToWire = new();

    /// <summary>
    /// Scans the specified assembly and registers all command types that are annotated with
    /// <see cref="CommandNameAttribute"/> and implement <see cref="ICommand"/>.
    /// </summary>
    /// <param name="assembly">
    /// The assembly to scan. This should typically be the shared contracts assembly that contains
    /// your command DTOs.
    /// </param>
    public void RegisterFromAssembly(Assembly assembly)
    {
        foreach (var t in assembly.GetTypes())
        {
            // Only concrete command types should be registered.
            if (t.IsAbstract) continue;

            // Only types that implement the command marker interface are considered commands.
            if (!typeof(ICommand).IsAssignableFrom(t)) continue;

            // Only types with an explicit wire identity participate in remote dispatch.
            var attr = t.GetCustomAttribute<CommandNameAttribute>();
            if (attr is null) continue;

            // Register in both directions to support send and receive scenarios.
            _wireToType[(attr.Name, attr.Version)] = t;
            _typeToWire[t] = (attr.Name, attr.Version);
        }
    }

    /// <summary>
    /// Resolves a wire-level command identity to a CLR type.
    /// </summary>
    /// <param name="name">The wire-level command name.</param>
    /// <param name="version">The wire-level command contract version.</param>
    /// <returns>The CLR type corresponding to the command identity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no command type has been registered for the provided (name, version) pair.
    /// </exception>
    public Type Resolve(string name, int version)
        => _wireToType.TryGetValue((name, version), out var t)
            ? t
            : throw new InvalidOperationException($"Unknown command: {name} v{version}");

    /// <summary>
    /// Gets the wire-level identity for the specified command CLR type.
    /// </summary>
    /// <param name="commandType">The CLR type of the command.</param>
    /// <returns>The wire-level (name, version) pair for the command type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the provided command type has not been registered.
    /// </exception>
    public (string name, int version) GetWire(Type commandType)
        => _typeToWire.TryGetValue(commandType, out var w)
            ? w
            : throw new InvalidOperationException($"Unregistered command type: {commandType.Name}");
}
