using System.Linq.Expressions;

namespace CosmosKit.Implementations;

internal sealed class RepositoryProxy<TEntity> : IRepository<TEntity> where TEntity : EntityBase
{
    private readonly IRepository<TEntity> _innerRepository;
    private readonly UnitOfWork _unitOfWork;

    public RepositoryProxy(IRepository<TEntity> innerRepository, UnitOfWork unitOfWork)
    {
        _innerRepository = innerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) =>
        await _innerRepository.GetAsync(predicate, cancellationToken);

    public IAsyncEnumerable<TEntity> StreamAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) =>
        _innerRepository.StreamAsync(predicate, cancellationToken);

    public async Task<TEntity?> GetByAsync(TEntity entity, CancellationToken cancellationToken) =>
        await _innerRepository.GetByAsync(entity, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) =>
        await _innerRepository.AnyAsync(predicate, cancellationToken);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_unitOfWork.IsInTransaction)
        {
            RepositoryHelper.SetEntityDefaults(entity);

            _unitOfWork.AddOperation(entity, (batch, e) => batch.CreateItem(e));

            return entity;
        }
        else
        {
            return await _innerRepository.AddAsync(entity, cancellationToken);
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_unitOfWork.IsInTransaction)
        {
            RepositoryHelper.SetEntityDefaults(entity);

            _unitOfWork.AddOperation(entity, (batch, e) => batch.ReplaceItem(e.Id, e));

            return entity;
        }
        else
        {
            return await _innerRepository.UpdateAsync(entity, cancellationToken);
        }
    }

    public async Task<TEntity> UpsertAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_unitOfWork.IsInTransaction)
        {
            RepositoryHelper.SetEntityDefaults(entity);

            _unitOfWork.AddOperation(entity, (batch, e) => batch.UpsertItem(e));

            return entity;
        }
        else
        {
            return await _innerRepository.UpsertAsync(entity, cancellationToken);
        }
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_unitOfWork.IsInTransaction)
        {
            _unitOfWork.AddOperation(entity, (batch, e) => batch.DeleteItem(entity.Id.ToString()));
        }
        else
        {
            await _innerRepository.DeleteAsync(entity, cancellationToken);
        }
    }
}