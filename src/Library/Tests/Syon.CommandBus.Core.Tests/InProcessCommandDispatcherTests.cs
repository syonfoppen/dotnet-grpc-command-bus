using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.InProcess;

namespace Syon.CommandBus.Core.Tests;

public sealed class InProcessCommandDispatcherTests
{
    [Fact]
    public async Task SendAsync_Returns_NO_HANDLER_When_Handler_Is_Not_Registered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandIdGenerator, FixedCommandIdGenerator>();

        var provider = services.BuildServiceProvider();
        var dispatcher = new InProcessCommandDispatcher(provider, provider.GetRequiredService<ICommandIdGenerator>());

        var result = await dispatcher.SendAsync(new TestCommand());

        Assert.Equal(DispatchStatus.Failed, result.Status);
        Assert.Equal("NO_HANDLER", result.ErrorCode);
        Assert.Equal("fixed-id", result.CommandId);
    }

    [Fact]
    public async Task SendAsync_Executes_Handler_When_Registered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandIdGenerator, FixedCommandIdGenerator>();
        services.AddScoped<ICommandHandler<TestCommand>, TestHandler>();

        var provider = services.BuildServiceProvider();
        var dispatcher = new InProcessCommandDispatcher(provider, provider.GetRequiredService<ICommandIdGenerator>());

        var result = await dispatcher.SendAsync(new TestCommand());

        Assert.Equal(DispatchStatus.Succeeded, result.Status);
        Assert.Equal("fixed-id", result.CommandId);
    }

    private sealed class FixedCommandIdGenerator : ICommandIdGenerator
    {
        // English comment: Deterministic IDs make tests stable.
        public string NewId() => "fixed-id";
    }

    private sealed class TestCommand : ICommand { }

    private sealed class TestHandler : ICommandHandler<TestCommand>
    {
        public Task<DispatchResult> HandleAsync(TestCommand command, CommandContext context, CancellationToken ct)
            => Task.FromResult(DispatchResult.Success(context.CommandId));
    }
}
