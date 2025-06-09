using CosmosKit.Implementations;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CosmosKit;

public static class DependencyInjection
{
    /// <summary>
    /// Registers CosmosKit services with the specified database ID and entity containers.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="databaseId">The ID of the Cosmos DB database.</param>
    /// <param name="entityContainers">A collection of entity containers specifying entity types, container names, and partition keys.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCosmosKit(this IServiceCollection services, string databaseId, IEnumerable<EntityContainer> entityContainers)
    {
        RepositoryHelper.DatabaseId = databaseId;

        var registeredContainers = entityContainers.ToDictionary(e => e.EntityType, e => e.ContainerName);
        var registeredPartitionKeys = entityContainers.ToDictionary(e => e.EntityType, e => e.EntityType.GetProperty(e.PartitionKey)!);

        services.AddSingleton(new ContainerResolver()
        {
            RegisteredContainers = registeredContainers,
            RegisteredPartitionKeys = registeredPartitionKeys
        });

        services.AddScoped<CosmosLinqQuery>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers CosmosKit services with the specified database ID, entity containers, and custom JSON serializer options.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="databaseId">The ID of the Cosmos DB database.</param>
    /// <param name="entityContainers">A collection of entity containers specifying entity types, container names, and partition keys.</param>
    /// <param name="configureJson">An action to configure the JSON serializer options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCosmosKit(this IServiceCollection services, string databaseId, IEnumerable<EntityContainer> entityContainers, Action<JsonSerializerOptions> configureJson)
    {
        services.AddSingleton<CosmosSerializer>(sp =>
        {
            var options = new JsonSerializerOptions();
            configureJson(options);
            var logger = sp.GetRequiredService<ILogger<CosmosSerializer>>();
            logger.LogWarning("You configured System.Text.Json, but make sure you also register CosmosClient with this serializer!");

            return new CosmosSystemTextJsonSerializer(options);
        });

        return AddCosmosKit(services, databaseId, entityContainers);
    }
}

/// <summary>
/// Represents an entity container with its associated type, container name, and partition key.
/// </summary>
/// <param name="EntityType">The type of the entity.</param>
/// <param name="ContainerName">The name of the Cosmos DB container.</param>
/// <param name="PartitionKey">The partition key for the entity.</param>
public record EntityContainer(Type EntityType, string ContainerName, string PartitionKey);