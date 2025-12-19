using Syon.CommandBus.Abstractions;

namespace Syon.CommandBus.Core;

/// <summary>
/// Default implementation of <see cref="IIdempotencyKeyProvider"/>.
///
/// This implementation disables idempotency by always returning <c>null</c>.
/// It is intended as a safe default for scenarios where idempotent command
/// execution is not required or is handled by other means.
/// </summary>
public sealed class DefaultIdempotencyKeyProvider : IIdempotencyKeyProvider
{
    /// <summary>
    /// Returns <c>null</c> to indicate that no idempotency key should be applied
    /// for the specified command.
    /// </summary>
    /// <param name="command">
    /// The command for which an idempotency key could be generated.
    /// This parameter is ignored by this implementation.
    /// </param>
    /// <returns>
    /// Always returns <c>null</c>, disabling idempotency for the command.
    /// </returns>
    public string? GetKey(ICommand command) => null;
}
