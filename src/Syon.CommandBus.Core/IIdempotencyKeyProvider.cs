using Syon.CommandBus.Abstractions;

namespace Syon.CommandBus.Core;

/// <summary>
/// Provides idempotency keys for commands.
///
/// Idempotency keys are used to detect and safely ignore duplicate command
/// submissions, which can occur due to retries, network failures, or
/// client-side timeouts.
///
/// Implementations of this interface determine whether a command should
/// be treated as idempotent and how the idempotency key is derived.
/// </summary>
public interface IIdempotencyKeyProvider
{
    /// <summary>
    /// Returns an idempotency key for the specified command.
    /// </summary>
    /// <param name="command">
    /// The command for which an idempotency key should be generated.
    /// </param>
    /// <returns>
    /// A stable, deterministic idempotency key, or <c>null</c> to indicate
    /// that idempotency should be disabled for this command.
    /// </returns>
    string? GetKey(ICommand command);
}
