using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;

namespace Syon.CommandBus.InProcess;

/// <summary>
/// Dependency injection registration helpers for the in-process command bus.
///
/// This extension registers the in-process dispatcher and the default infrastructure services
/// required to generate command identifiers and optionally support idempotency.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-process <see cref="ICommandDispatcher"/> implementation.
    ///
    /// This setup is intended for applications where commands are handled in the same runtime
    /// (no network hop). Command handlers must still be registered separately, typically via
    /// <c>AddCommandHandlersFromAssembly(...)</c>.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandBusInProcess(this IServiceCollection services)
    {
        // Default ID strategy uses GUID v7 for time-ordered IDs.
        services.AddSingleton<ICommandIdGenerator, GuidCommandIdGenerator>();

        // Default idempotency strategy disables idempotency unless overridden by the application.
        services.AddSingleton<IIdempotencyKeyProvider, DefaultIdempotencyKeyProvider>();

        // Scoped is a safe default because handlers often depend on scoped services.
        services.AddScoped<ICommandDispatcher, InProcessCommandDispatcher>();

        return services;
    }
}
