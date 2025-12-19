# dotnet-distributed-command-bus

A lightweight command bus for .NET that supports both in-process and gRPC-based command dispatch,
while keeping application code fully transport-agnostic.

The goal of this project is to make remote command execution feel like in-process dispatch,
without hiding the realities of distributed systems.

---

## Key Concepts

- **Commands represent intent**, not queries
- **Exactly one handler per command**
- **Explicit command contracts** using name + version
- **Transport-agnostic dispatching**
- **Deterministic idempotency**
- **No magic, no code generation, no framework lock-in**

---

## Architecture Overview

```
Application Code
       |
       v
ICommandDispatcher
       |
       +-- InProcessCommandDispatcher
       |
       +-- GrpcCommandDispatcher
               |
               v
         Remote Command Handlers
```

Your application code depends only on `ICommandDispatcher`.
Switching between in-process and gRPC dispatch is a configuration change.

---

## Packages / Projects

- **CommandBus.Abstractions**
  Core interfaces and contracts

- **CommandBus.Core**
  Command registry, ID generation, idempotency abstractions

- **CommandBus.InProcess**
  In-process dispatcher implementation

- **CommandBus.Grpc**
  gRPC dispatcher and server endpoint

- **CommandBus.Shared**
  Example shared command contracts

---

## Defining a Command

```csharp
[CommandName("CreateCustomer", version: 1)]
public sealed class CreateCustomerCommand : ICommand
{
    public string CustomerId { get; set; } = "";
    public string Name { get; set; } = "";
}
```

Commands:
- Are plain DTOs
- Must implement `ICommand`
- Must have a stable `CommandName`

---

## Handling a Command

```csharp
public sealed class CreateCustomerHandler
    : ICommandHandler<CreateCustomerCommand>
{
    public Task<DispatchResult> HandleAsync(
        CreateCustomerCommand command,
        CommandContext context,
        CancellationToken ct)
    {
        // Business logic here
        return Task.FromResult(
            DispatchResult.Success(context.CommandId));
    }
}
```

Handlers:
- Contain application or domain logic
- Return `DispatchResult` instead of throwing for expected failures

---

## In-Process Usage

```csharp
services.AddCommandBusCore(
    typeof(CommandContractsAssembly).Assembly);

services.AddCommandHandlersFromAssembly(
    Assembly.GetExecutingAssembly());

services.AddCommandBusInProcess();
```

---

## gRPC Usage

### Receiver (Server)

```csharp
builder.Services.AddGrpc();

builder.Services.AddCommandBusCore(
    typeof(CommandContractsAssembly).Assembly);

builder.Services.AddCommandHandlersFromAssembly(
    Assembly.GetExecutingAssembly());

app.MapGrpcService<CommandBusGrpcService>();
```

### Transmitter (Client)

```csharp
services.AddCommandBusCore(
    typeof(CommandContractsAssembly).Assembly);

services.AddCommandBusGrpcClient(
    new Uri("https://localhost:7003"));
```

---

## Idempotency

Idempotency is explicit and deterministic.

```csharp
public sealed class BusinessKeyIdempotencyKeyProvider
    : IIdempotencyKeyProvider
{
    public string? GetKey(ICommand command) =>
        command switch
        {
            CreateCustomerCommand c =>
                $"CreateCustomer:{c.CustomerId}",

            _ => null
        };
}
```

Register the provider on **both client and server**.

---

## Hobby Project Disclaimer

This project is a personal hobby project created to explore and better understand
command-based architectures, dependency inversion, and the practical trade-offs
between in-process and distributed command dispatch.

While the code is written with care and architectural intent, it is primarily meant
for learning, experimentation, and discussion rather than production use as-is.

---

## What This Is Not

- Not CQRS
- Not Event Sourcing
- Not a workflow engine
- Not a message broker replacement

This library focuses purely on **command dispatch**.

---

## Design Principles

- Explicit over implicit
- Fail fast on misconfiguration
- Distributed systems are not transparent
- Transport concerns belong at the edge
- Application code should not care how commands are delivered

---

## License

MIT

---

## Status

This project is stable enough for experimentation and internal tooling.
