using Syon.CommandBus.Core;
using Syon.CommandBus.Examples.Commands;
using Syon.CommandBus.Grpc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Register command contracts (DTOs) from the shared contracts assembly.
// This is the only assembly that should be scanned for command definitions.
builder.Services.AddCommandBusCore(typeof(CommandContractsAssembly).Assembly);

// Register all command handlers from the server assembly.
builder.Services.AddCommandHandlersFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();

app.MapGrpcService<CommandBusGrpcService>();
app.MapGet("/", () => "gRPC server is running.");

app.UseHttpsRedirection();
app.Run();
