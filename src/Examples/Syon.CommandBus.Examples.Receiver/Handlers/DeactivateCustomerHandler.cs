using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Examples.Commands;

namespace Syon.CommandBus.Examples.Receiver.Handlers;

public sealed class DeactivateCustomerHandler
    : ICommandHandler<DeactivateCustomerCommand>
{
    public Task<DispatchResult> HandleAsync(
        DeactivateCustomerCommand command,
        CommandContext context,
        CancellationToken ct)
    {
        Console.WriteLine(
            $"[{context.CommandId}] Deactivating customer {command.CustomerId}. Reason: {command.Reason}");

        return Task.FromResult(DispatchResult.Success(context.CommandId));
    }
}
