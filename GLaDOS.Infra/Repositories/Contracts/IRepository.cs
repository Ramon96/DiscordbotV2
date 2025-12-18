using GLaDOS.Domain;
using GLaDOS.Infra.Specifications.Contracts;

namespace GLaDOS.Infra.Repositories.Contracts;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByExpressionAsync(ISpecificationBase<TEntity> specification, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> SaveChangesAsync(TEntity entity, CancellationToken cancellationToken = default);
}