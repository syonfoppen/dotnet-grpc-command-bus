using Syon.CommandBus.Abstractions;

namespace Syon.CommandBus.Examples.Commands;

[CommandName("CreateCustomer", version: 1)]
public sealed class CreateCustomerCommand : ICommand
{
    public string CustomerId { get; set; } = "";
    public string Name { get; set; } = "";
}
