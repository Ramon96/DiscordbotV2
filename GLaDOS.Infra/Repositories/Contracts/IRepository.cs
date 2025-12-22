using GLaDOS.Domain;
using GLaDOS.Infra.Specifications.Contracts;

namespace GLaDOS.Infra.Repositories.Contracts;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByExpressionAsync(ISpecificationBase<TEntity> specification, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}