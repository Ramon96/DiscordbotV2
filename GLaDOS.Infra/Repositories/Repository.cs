using GLaDOS.Domain;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Infra.Repositories.Contracts;
using GLaDOS.Infra.Specifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Infra.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    private readonly ApplicationDbContext _context;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().ToListAsync(cancellationToken);
    }

    public async Task<TEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<TEntity>().FindAsync([id], cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Entity of type {typeof(TEntity).Name} with ID {id} was not found.");
        }

        return entity;
    }

    public async Task<TEntity?> GetByExpressionAsync(ISpecificationBase<TEntity> specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var query = _context.Set<TEntity>().AsQueryable();
        query = specification.Apply(query);
        
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<TEntity> SaveChangesAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        entity.ModifiedDate = DateTime.Now;
   
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}