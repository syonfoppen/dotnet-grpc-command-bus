using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using System.Reflection;

namespace Syon.CommandBus.Core;

/// <summary>
/// Provides extension methods for registering command handlers in the dependency injection container.
///
/// The command bus relies on DI to resolve <see cref="ICommandHandler{TCommand}"/> implementations at runtime.
/// These helpers scan assemblies for handler implementations and register them with a configurable lifetime.
/// </summary>
public static class CommandHandlerRegistrationExtensions
{
    /// <summary>
    /// Scans the specified assembly for implementations of <see cref="ICommandHandler{TCommand}"/>
    /// and registers them in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add handler registrations to.</param>
    /// <param name="assembly">
    /// The assembly to scan for handler implementations. Typically this is the application assembly
    /// that contains your handler classes (not the shared contracts assembly).
    /// </param>
    /// <param name="lifetime">
    /// The DI lifetime for registered handlers. Scoped is typically the best default for server applications,
    /// because handlers often depend on scoped services such as DbContexts.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        // We only care about concrete, non-generic classes that can be instantiated.
        var candidates = assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsGenericTypeDefinition);

        foreach (var implType in candidates)
        {
            // A single class could implement multiple ICommandHandler<T> interfaces.
            var handlerInterfaces = implType
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                .ToArray();

            if (handlerInterfaces.Length == 0)
                continue;

            foreach (var serviceType in handlerInterfaces)
            {
                // Register the handler interface to the concrete implementation.
                var descriptor = new ServiceDescriptor(serviceType, implType, lifetime);

                // Avoid duplicates when the same assembly is scanned multiple times.
                if (!services.Any(d =>
                        d.ServiceType == descriptor.ServiceType &&
                        d.ImplementationType == descriptor.ImplementationType))
                {
                    services.Add(descriptor);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Scans multiple assemblies for implementations of <see cref="ICommandHandler{TCommand}"/>
    /// and registers them in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add handler registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for handler implementations.</param>
    /// <param name="lifetime">The DI lifetime to use for handler registrations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandHandlersFromAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        // Register handlers from each assembly using the same lifetime.
        foreach (var asm in assemblies)
            services.AddCommandHandlersFromAssembly(asm, lifetime);

        return services;
    }

    /// <summary>
    /// Scans the specified assemblies for command handlers and registers them with the given lifetime.
    /// </summary>
    /// <param name="services">The service collection to add handler registrations to.</param>
    /// <param name="lifetime">The DI lifetime to use for handler registrations.</param>
    /// <param name="assemblies">The assemblies to scan for handler implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandHandlersFromAssemblies(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies)
        => services.AddCommandHandlersFromAssemblies(assemblies, lifetime);

    /// <summary>
    /// Scans the specified assemblies for command handlers and registers them with <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    /// <param name="services">The service collection to add handler registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for handler implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddCommandHandlersFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddCommandHandlersFromAssemblies(assemblies, ServiceLifetime.Scoped);
}
