namespace CosmosKit;

public interface IUnitOfWork
{
    /// <summary>
    /// Retrieves a repository for the specified entity type.
    /// </summary>
    /// <remarks>This method ensures that a single repository instance is maintained per entity type. If a
    /// repository for the specified entity type does not already exist, it is created and cached for future
    /// use.</remarks>
    /// <typeparam name="TEntity">The type of the entity for which the repository is requested. Must inherit from <see cref="EntityBase"/>.</typeparam>
    /// <returns>An instance of <see cref="IRepository{TEntity}"/> for the specified entity type. If a repository for the entity
    /// type already exists, the existing instance is returned; otherwise, a new repository is created and returned.</returns>
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase;

    /// <summary>
    /// Begins a new transaction asynchronously.
    /// </summary>
    /// <remarks>This method initializes a new transaction, clearing any pending operations.  It ensures that
    /// only one transaction can be active at a time.
    /// <para>IMPORTANT: CosmosDB transaction batches are only valid for operations within the same container and partition key.</para></remarks>
    /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a transaction is already in progress.</exception>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction by executing all pending operations in a batch.
    /// </summary>
    /// <remarks>This method processes all pending operations grouped by container and partition key,  and
    /// executes them as transactional batches in Azure Cosmos DB. If the batch size exceeds  the Cosmos DB limit of 100
    /// operations, the transaction is rolled back, and an exception is thrown.  After the transaction is committed, the
    /// pending operations are cleared, and the transaction state is reset.
    /// <para>IMPORTANT: CosmosDB transaction batches are only valid for operations within the same container and partition key.</para></remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is in progress or if the batch size exceeds the Cosmos DB limit of 100 operations.</exception>
    /// <exception cref="CosmosException">Thrown if any batch operation fails to execute successfully in Cosmos DB.</exception>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction, discarding all pending operations.
    /// </summary>
    /// <remarks>This method clears all operations that were staged during the current transaction  and resets
    /// the transaction state. Ensure that a transaction is in progress before  calling this method.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is currently in progress.</exception>
    Task RollbackTransactionAsync();
}
