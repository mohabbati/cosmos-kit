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


## 🧩 Optional: Custom JSON Serialization

You can enable **System.Text.Json** support by using the overload that accepts a `JsonSerializerOptions` configuration:

```csharp
builder.AddCosmosKit(
    databaseId: "AppDb",
    entityContainers: new[]
    {
        new DependencyInjection.EntityContainer(typeof(Order), "orders", "TenantId")
    },
    configureJson: options =>
    {
        options.TypeInfoResolver = MyAppJsonContext.Default;
        options.Converters.Add(new MyPolymorphicConverter());
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

> ⚠️ **Important:** When using this overload, you must also ensure that the `CosmosClient` is constructed with the registered `CosmosSerializer`:

```csharp
builder.Services.AddSingleton(sp =>
{
    var serializer = sp.GetRequiredService<CosmosSerializer>();

    return new CosmosClient(connectionString, new CosmosClientOptions
    {
        Serializer = serializer,
        ApplicationName = "MyApp"
    });
});
```

## ✅ Entity Model Requirements

> 🔑 **Cosmos DB requires the `id` field to be lowercase in JSON**.

If you're using `System.Text.Json` (recommended):

```csharp
using System.Text.Json.Serialization;

public abstract class EntityBase
{
    [JsonPropertyName("id")] // Required by Cosmos DB
    public string Id { get; set; } = default!;
}
```

> ⚠️ **If you're using `Newtonsoft.Json`**, override the `Id` property in your entity and annotate it like this:

```csharp
using Newtonsoft.Json;

public class MyEntity : EntityBase
{
    [JsonProperty("id")]
    public new string Id { get; set; } = default!;
}
```
## Usage

`CosmosKit` supports both direct repository usage and transactional operations via `IUnitOfWork`.

### 🔹 Using Repository (no transaction)

Use the repository directly if you don't need transaction support. This is efficient for most single-entity operations:

```csharp
public class CustomerService
{
    private readonly IRepository<Customer> _repo;

    public CustomerService(IRepository<Customer> repo)
    {
        _repo = repo;
    }

    public async Task CreateCustomerAsync(Customer customer, CancellationToken ct)
    {
        await _repo.AddAsync(customer, ct);
    }
}
```

### 🔹 Using IUnitOfWork (transactional batch)

`IUnitOfWork` enables grouping operations into a transactional batch **within the same container and partition key**. This is especially useful when consistency is critical:

> ⚠️ **Important:** Cosmos DB transactional batch only works when **all operations target the same container and the same partition key**.
>
> If you add entities to different containers or with different partition keys, CosmosKit will apply **independent transactions per container/partition key combination**.

```csharp
public class OrderService
{
    private readonly IUnitOfWork _uow;

    public OrderService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task PlaceOrderAsync(Order order, Customer customer, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync();

        var orderRepo = _uow.GetRepository<Order>();
        var customerRepo = _uow.GetRepository<Customer>();

        await orderRepo.AddAsync(order, ct);
        await customerRepo.AddAsync(customer, ct);

        await _unitOfWork.CommitTransactionAsync();
    }
}
```

`GetRepository` returns a repository proxy that automatically participates in the unit of work transaction, if one is started.