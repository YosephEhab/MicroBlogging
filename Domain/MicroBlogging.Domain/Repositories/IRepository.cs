using System.Linq.Expressions;
using MicroBlogging.Domain.Entities;

namespace MicroBlogging.Domain.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetById(Guid id);
    Task<List<T>?> GetByIds(List<Guid> guids);
    Task<List<T>> GetAll();
    Task<List<T>> GetByCondition(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefault(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetNextN(Expression<Func<T, bool>> predicate, int n);

    Task Add(T entity);
    Task Update(T entity);
    Task Delete(T entity);
    Task SaveChanges();
}