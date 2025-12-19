using Syon.CommandBus.Abstractions;

namespace Syon.CommandBus.IntegrationTests.Commands;

[CommandName("DeactivateCustomer", version: 1)]
public sealed class DeactivateCustomerCommand : ICommand
{
    public string CustomerId { get; set; } = "";
    public string Reason { get; set; } = "";
}
