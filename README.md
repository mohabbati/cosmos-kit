# CosmosKit

CosmosKit provides Repository and Unit of Work patterns on top of the Azure Cosmos DB SDK. It simplifies data access by encapsulating common operations and transactional batch logic.

## Installation

Add a reference to the package (once published) or include the project in your solution.

```bash
<PackageReference Include="CosmosKit" Version="*" />
```

## Configuration

1. Register `CosmosClient` in your service container.
2. Call `AddCosmosKit` specifying the database id and your entity containers.

```csharp
builder.Services.AddSingleton(new CosmosClient(connectionString));

builder.AddCosmosKit(
    databaseId: "AppDb",
    new []
    {
        new DependencyInjection.EntityContainer(typeof(Order), "orders", "TenantId"),
        new DependencyInjection.EntityContainer(typeof(Customer), "customers", "TenantId")
    });
```

## Usage

Obtain an `IUnitOfWork` from dependency injection and use repositories to perform operations. Use transactions when you need to group multiple operations within the same partition key.

```csharp
public class OrderService
{
    private readonly IUnitOfWork _uow;

    public OrderService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        var repo = _uow.GetRepository<Order>();
        await repo.AddAsync(order, ct);
    }
}
```

`GetRepository` returns a repository proxy that automatically participates in transactions started on the unit of work.

## Streaming Queries

`IRepository` exposes `StreamAsync` to asynchronously enumerate large query results without buffering:

```csharp
await foreach(var order in repo.StreamAsync(o => o.Status == "new", ct))
{
    // process order
}
```

## License

This project is licensed under the MIT License.
