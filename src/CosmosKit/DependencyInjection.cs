using CosmosKit.Implementations;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<CosmosLinqQuery>();
        builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
            sp.GetRequiredService<CosmosClient>(),
            sp.GetRequiredService<ContainerResolver>(),
            databaseId,
            sp.GetRequiredService<CosmosLinqQuery>(),
            sp.GetRequiredService<ILogger<UnitOfWork>>()));

        return builder;
    }

    public static IHostApplicationBuilder AddCosmosKit(this IHostApplicationBuilder builder, string databaseId, IEnumerable<EntityContainer> entityContainers, Action<JsonSerializerOptions> configureJson)
    {
        builder.Services.AddSingleton<CosmosSerializer>(sp =>
        {
            var options = new JsonSerializerOptions();
            configureJson(options);

            var logger = sp.GetRequiredService<ILogger<CosmosSerializer>>();
            logger.LogWarning("You configured System.Text.Json, but make sure you also register CosmosClient with this serializer!");

            return new CosmosSystemTextJsonSerializer(options);
        });

        return AddCosmosKit(builder, databaseId, entityContainers);
    }

    public record EntityContainer(Type EntityType, string ContainerName, string PartitionKey);
}