using CosmosKit.Implementations;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmosKit;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddCosmosKit(this IHostApplicationBuilder builder, string databaseId, IEnumerable<EntityContainer> entityContainers)
    {
        var registeredContainers = entityContainers.ToDictionary(e => e.EntityType, e => e.ContainerName);
        var registeredPartitionKeys = entityContainers.ToDictionary(e => e.EntityType, e => e.EntityType.GetProperty(nameof(e.PartitionKey))!);

        builder.Services.AddSingleton(new ContainerResolver()
        {
            RegisteredContainers = registeredContainers,
            RegisteredPartitionKeys = registeredPartitionKeys
        });
        builder.Services.AddScoped<CosmosLinqQuery>();
        builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
            sp.GetRequiredService<CosmosClient>(),
            sp.GetRequiredService<ContainerResolver>(),
            databaseId,
            sp.GetRequiredService<CosmosLinqQuery>(),
            sp.GetRequiredService<ILogger<UnitOfWork>>()));

        return builder;
    }

    public record EntityContainer(Type EntityType, string ContainerName, string PartitionKey);
}