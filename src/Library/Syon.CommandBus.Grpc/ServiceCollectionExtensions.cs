using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Core;
using Syon.CommandBus.Grpc.V1;

namespace Syon.CommandBus.Grpc;

/// <summary>
/// Dependency injection registration helpers for the command bus.
///
/// These extensions provide a single, consistent way to register the command bus core services
/// (registry, id generation, idempotency strategy) and the gRPC-based dispatcher implementation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the command bus core services.
    ///
    /// This includes:
    /// - <see cref="CommandTypeRegistry"/> configured by scanning the provided contract assemblies
    /// - <see cref="ICommandIdGenerator"/> for generating command identifiers
    /// - <see cref="IIdempotencyKeyProvider"/> for deriving idempotency keys (or disabling idempotency)
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="contractAssemblies">
    /// Assemblies that contain command contract types (DTOs) annotated with <see cref="CommandNameAttribute"/>.
    /// This should typically be the shared contracts assembly used by both transmitter and receiver.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandBusCore(
        this IServiceCollection services,
        params System.Reflection.Assembly[] contractAssemblies)
    {
        // Build a registry once and reuse it for both send and receive mapping.
        services.AddSingleton(sp =>
        {
            var reg = new CommandTypeRegistry();

            // Scan all provided contract assemblies to register command types.
            foreach (var asm in contractAssemblies)
                reg.RegisterFromAssembly(asm);

            return reg;
        });

        // Default ID strategy uses GUID v7 for time-ordered IDs.
        services.AddSingleton<ICommandIdGenerator, GuidCommandIdGenerator>();

        // Default idempotency strategy disables idempotency unless overridden by the application.
        services.AddSingleton<IIdempotencyKeyProvider, DefaultIdempotencyKeyProvider>();

        return services;
    }

    /// <summary>
    /// Registers the gRPC client and configures <see cref="ICommandDispatcher"/> to use
    /// <see cref="GrpcCommandDispatcher"/> for remote command dispatch.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="address">The base address of the remote gRPC command receiver.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandBusGrpcClient(this IServiceCollection services, Uri address)
    {
        // Register the generated gRPC client for the remote command bus endpoint.
        services.AddGrpcClient<CommandBusPipe.CommandBusPipeClient>(o => o.Address = address);

        // Scoped is a safe default in ASP.NET and console apps using scopes.
        services.AddScoped<ICommandDispatcher, GrpcCommandDispatcher>();

        return services;
    }
}
