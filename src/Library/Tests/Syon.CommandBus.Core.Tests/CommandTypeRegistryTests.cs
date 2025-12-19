using Syon.CommandBus.Abstractions;
using System.Reflection;
using ICommand = Syon.CommandBus.Abstractions.ICommand;

namespace Syon.CommandBus.Core.Tests;

public sealed class CommandTypeRegistryTests
{
    [Fact]
    public void RegisterFromAssembly_Registers_Commands_With_CommandNameAttribute()
    {
        var registry = new CommandTypeRegistry();

        registry.RegisterFromAssembly(Assembly.GetExecutingAssembly());

        var wire = registry.GetWire(typeof(TestCommand));

        Assert.Equal("TestCommand", wire.name);
        Assert.Equal(1, wire.version);
    }

    [Fact]
    public void Resolve_Returns_CommandType_When_Registered()
    {
        var registry = new CommandTypeRegistry();
        registry.RegisterFromAssembly(Assembly.GetExecutingAssembly());

        var type = registry.Resolve("TestCommand", 1);

        Assert.Equal(typeof(TestCommand), type);
    }

    [Fact]
    public void GetWire_Throws_When_Type_Is_Not_Registered()
    {
        var registry = new CommandTypeRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() => registry.GetWire(typeof(UnregisteredCommand)));

        Assert.Contains("Unregistered command type", ex.Message);
    }

    [Fact]
    public void Resolve_Throws_When_NameVersion_Is_Unknown()
    {
        var registry = new CommandTypeRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Resolve("DoesNotExist", 1));

        Assert.Contains("Unknown command", ex.Message);
    }

    // This command is valid because it implements ICommand and has CommandName.
    [CommandName("TestCommand", version: 1)]
    private sealed class TestCommand : ICommand
    {
        public string Value { get; set; } = "";
    }

    // This command implements ICommand but lacks CommandName, so it should not be registered.
    private sealed class UnregisteredCommand : ICommand
    {
    }
}
