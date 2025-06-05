using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CosmosKit;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddCosmosKit(this IHostApplicationBuilder builder, IEnumerable<EntityContainer> entityContainers)
    {
        var registeredContainers = entityContainers.ToDictionary(e => e.EntityType, e => e.ContainerName);
        var registeredPartitionKeys = entityContainers.ToDictionary(e => e.EntityType, e => e.EntityType.GetProperty(nameof(e.PartitionKey))!);

        builder.Services.AddSingleton(new ContainerResolver()
        {
            RegisteredContainers = registeredContainers,
            RegisteredPartitionKeys = registeredPartitionKeys
        });

        return builder;
    }

    public record EntityContainer(Type EntityType, string ContainerName, string PartitionKey);
}