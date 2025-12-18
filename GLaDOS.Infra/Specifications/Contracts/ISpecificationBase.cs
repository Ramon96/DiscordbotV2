using System.Linq.Expressions;

namespace GLaDOS.Infra.Specifications.Contracts;

public interface ISpecificationBase<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    bool IsSatisfiedBy(object entity);
    IQueryable<T> Apply(IQueryable<T> query);
}