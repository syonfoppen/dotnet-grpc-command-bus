using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;

namespace Syon.CommandBus.Core.Tests;

public sealed class CommandHandlerRegistrationExtensionsTests
{
    [Fact]
    public void AddCommandHandlersFromAssembly_Registers_Handler_Interface()
    {
        var services = new ServiceCollection();

        services.AddCommandHandlersFromAssembly(typeof(TestHandler).Assembly);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<TestCommand>>();

        Assert.NotNull(handler);
        Assert.IsType<TestHandler>(handler);
    }

    [Fact]
    public void AddCommandHandlersFromAssembly_DoesNot_Register_Duplicates()
    {
        var services = new ServiceCollection();

        services.AddCommandHandlersFromAssembly(typeof(TestHandler).Assembly);
        services.AddCommandHandlersFromAssembly(typeof(TestHandler).Assembly);

        // Only one registration for ICommandHandler<TestCommand> -> TestHandler is expected.
        var count = services.Count(d =>
            d.ServiceType == typeof(ICommandHandler<TestCommand>) &&
            d.ImplementationType == typeof(TestHandler));

        Assert.Equal(1, count);
    }

    [CommandName("TestCommandForHandler", 1)]
    private sealed class TestCommand : ICommand { }

    private sealed class TestHandler : ICommandHandler<TestCommand>
    {
        public Task<DispatchResult> HandleAsync(TestCommand command, CommandContext context, CancellationToken ct)
            => Task.FromResult(DispatchResult.Success(context.CommandId));
    }
}
