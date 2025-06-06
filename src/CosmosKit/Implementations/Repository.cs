using Azure;
using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace CosmosKit.Implementations;

internal sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : EntityBase
{
    private readonly Container _container;
    private readonly CosmosLinqQuery _cosmosLinqQuery;
    private readonly ContainerResolver _containerResolver;

    public Repository(CosmosClient cosmosClient, ContainerResolver containerResolver, CosmosLinqQuery cosmosLinqQuery)
    {
        _container = cosmosClient.GetContainer(RepositoryHelper.DatabaseId, containerResolver.ResolveName(typeof(TEntity)));
        _cosmosLinqQuery = cosmosLinqQuery;
        _containerResolver = containerResolver;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        var response = await _container.CreateItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var partitionKeyValue = GetPartitionKeyValue(entity);

        await _container.DeleteItemAsync<TEntity>(entity.Id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
    }

    public async Task<TEntity?> GetByAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var partitionKeyValue = GetPartitionKeyValue(entity);

        try
        {
            var response = await _container.ReadItemAsync<TEntity>(entity.Id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        var queryable = _container.GetItemLinqQueryable<TEntity>(true)
                                  .Where(predicate);

        var feedIterator = _cosmosLinqQuery.GetFeedIterator(queryable);

        var results = new List<TEntity>();

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TEntity> response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async IAsyncEnumerable<TEntity> StreamAsync(Expression<Func<TEntity, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = _container.GetItemLinqQueryable<TEntity>(true)
                                  .Where(predicate);

        var feedIterator = _cosmosLinqQuery.GetFeedIterator(queryable);

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TEntity> response = await feedIterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        var response = await _container.ReplaceItemAsync(entity, entity.Id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<TEntity> UpsertAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        var response = await _container.UpsertItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);

        return response.Resource;
    }

    private string GetPartitionKeyValue(TEntity entity)
    {
        var partitionKeyValue = _containerResolver.ResolvePartitionKey(typeof(TEntity))?.GetValue(entity)?.ToString();

        if (partitionKeyValue == null)
        {
            throw new ArgumentException($"The property '{typeof(TEntity)}' does not exist or is null.");
        }

        return partitionKeyValue;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        var queryable = _container.GetItemLinqQueryable<TEntity>(true)
                                  .Where(predicate);

        var feedIterator = _cosmosLinqQuery.GetFeedIterator(queryable);

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TEntity> response = await feedIterator.ReadNextAsync(cancellationToken);
            if (response.Any())
                return true; 
        }

        return false;
    }

}
