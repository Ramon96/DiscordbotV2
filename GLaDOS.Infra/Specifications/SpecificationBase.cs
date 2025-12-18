using System.Linq.Expressions;
using GLaDOS.Infra.Specifications.Contracts;

namespace GLaDOS.Infra.Specifications;

public abstract class SpecificationBase<T> : ISpecificationBase<T>
{
    public abstract Expression<Func<T, bool>> Criteria { get; }

    public bool IsSatisfiedBy(object entity)
    {
        return entity is T typedEntity && Criteria.Compile()(typedEntity);
    }

    public IQueryable<T> Apply(IQueryable<T> query)
    {
        return query.Where(Criteria);
    }
}