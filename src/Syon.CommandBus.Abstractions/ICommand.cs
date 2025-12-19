namespace Syon.CommandBus.Abstractions;

/// <summary>
/// Marker interface for command messages.
///
/// A command represents an intention to perform an action that changes system state.
/// Commands are immutable intent messages and are handled by exactly one command handler.
///
/// This interface is intentionally empty and exists only to provide
/// strong typing and clear semantic meaning within the command bus.
/// </summary>
public interface ICommand
{
}
