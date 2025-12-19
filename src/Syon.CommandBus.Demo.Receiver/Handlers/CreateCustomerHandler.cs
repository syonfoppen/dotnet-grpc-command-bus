using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Shared.Commands;

namespace Syon.CommandBus.Demo.Receiver.Handlers;

public sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand>
{
    public Task<DispatchResult> HandleAsync(CreateCustomerCommand command, CommandContext context, CancellationToken ct)
    {
        Console.WriteLine($"[{context.CommandId}] Creating customer {command.CustomerId} ({command.Name})");
        return Task.FromResult(DispatchResult.Success(context.CommandId));
    }
}
