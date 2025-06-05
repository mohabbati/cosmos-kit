using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CosmosKit.Implementations;

// IMPORTANT Note: CosmosDB transaction batches are only valid for operations within the same container and partition key.

internal sealed class UnitOfWork(CosmosClient cosmosClient, ContainerResolver containerResolver, string databaseId, CosmosLinqQuery cosmosLinqQuery, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private readonly ConcurrentDictionary<(string Container, PartitionKey PartitionKey), ConcurrentBag<Func<TransactionalBatch, TransactionalBatch>>> _pendingOperations = new();

    public bool IsInTransaction { get; private set; } = false;

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase
    {
        if (_repositories.TryGetValue(typeof(TEntity), out var repository))
        {
            return (IRepository<TEntity>)repository;
        }

        var repositoryProxy = new RepositoryProxy<TEntity>(new Repository<TEntity>(cosmosClient, containerResolver, databaseId, cosmosLinqQuery), this);

        _repositories.TryAdd(typeof(TEntity), repositoryProxy);

        return repositoryProxy;
    }

    public Task BeginTransactionAsync()
    {
        if (IsInTransaction)
        {
            throw new InvalidOperationException("Transaction already in progress.");
        }
        IsInTransaction = true;
        _pendingOperations.Clear();

        return Task.CompletedTask;
    }

    internal void AddOperation<TEntity>(TEntity entity, Func<TransactionalBatch, TEntity, TransactionalBatch> operation) where TEntity : class
    {
        if (!IsInTransaction)
            throw new InvalidOperationException("No transaction in progress.");

        var containerName = containerResolver.ResolveName(typeof(TEntity));
        var partitionKeyProperty = containerResolver.ResolvePartitionKey(typeof(TEntity));

        var partitionKeyValue = partitionKeyProperty.GetValue(entity)?.ToString();
        if (partitionKeyValue is null)
        {
            throw new ArgumentException($"The property '{typeof(TEntity)}' does not exist or is null.");
        }

        var partitionKey = new PartitionKey(partitionKeyValue);
        var key = (containerName, partitionKey);

        if (!_pendingOperations.TryGetValue(key, out var operations))
        {
            operations = [];
            _pendingOperations[key] = operations;
        }

        operations.Add(batch => operation(batch, entity));
    }

    public async Task CommitTransactionAsync()
    {
        if (!IsInTransaction)
            throw new InvalidOperationException("No transaction in progress");

        // Check batch size limits
        if (_pendingOperations.Values.Count >= 100)
        {
            await RollbackTransactionAsync();
            throw new InvalidOperationException("Cosmos DB batch size limit (100) exceeded.");
        }    

        try
        {
            foreach (var ((containerName, partitionKey), operations) in _pendingOperations)
            {
                var container = cosmosClient.GetContainer(databaseId, containerName);
                var batch = container.CreateTransactionalBatch(partitionKey);

                foreach (var operation in operations)
                {
                    batch = operation(batch);
                }

                using var response = await batch.ExecuteAsync();
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("An error occurred: {Message}", response.ErrorMessage);
                    throw new CosmosException(
                        response.ErrorMessage,
                        response.StatusCode,
                        (int)response.StatusCode,
                        response.ActivityId,
                        response.RequestCharge);
                }
            }
        }
        finally
        {
            _pendingOperations.Clear();
            IsInTransaction = false;
        }
    }

    public Task RollbackTransactionAsync()
    {
        if (!IsInTransaction)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        _pendingOperations.Clear();
        IsInTransaction = false;

        return Task.CompletedTask;
    }
}