using Microsoft.Extensions.DependencyInjection;
using Syon.CommandBus.Abstractions;
using Syon.CommandBus.Examples.Commands;
using Syon.CommandBus.Grpc;

namespace Syon.CommandBus.Examples.Transmitter;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Register the command bus core services and scan the shared contracts assembly once.
        services.AddCommandBusCore(typeof(CommandContractsAssembly).Assembly);

        // Register the gRPC dispatcher and configure the remote receiver address.
        services.AddCommandBusGrpcClient(new Uri("https://localhost:7003"));

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var cmd = new CreateCustomerCommand
        {
            CustomerId = "C123",
            Name = "Cars"
        };

        Console.WriteLine("Press any key to send a create command...");
        Console.ReadKey();

        var result = await dispatcher.SendAsync(cmd);

        Console.WriteLine($"Status: {result.Status} CommandId: {result.CommandId}");
        if (result.Status == DispatchStatus.Failed)
        {
            Console.WriteLine($"Error: {result.ErrorCode} {result.ErrorMessage}");
        }

        Console.WriteLine("Press any key to send deactivate command...");
        Console.ReadKey();

        var deactivate = new DeactivateCustomerCommand
        {
            CustomerId = "C123",
            Reason = "Customer requested account closure"
        };

        result = await dispatcher.SendAsync(deactivate);

        Console.WriteLine($"Status: {result.Status} CommandId: {result.CommandId}");
        if (result.Status == DispatchStatus.Failed)
        {
            Console.WriteLine($"Error: {result.ErrorCode} {result.ErrorMessage}");
        }

        Console.ReadKey();
    }
}
