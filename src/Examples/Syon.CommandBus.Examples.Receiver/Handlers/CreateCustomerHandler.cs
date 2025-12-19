using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Examples.Commands;

namespace Syon.CommandBus.Examples.Receiver.Handlers;

public sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand>
{
    public Task<DispatchResult> HandleAsync(CreateCustomerCommand command, CommandContext context, CancellationToken ct)
    {
        Console.WriteLine($"[{context.CommandId}] Creating customer {command.CustomerId} ({command.Name})");
        return Task.FromResult(DispatchResult.Success(context.CommandId));
    }
}
