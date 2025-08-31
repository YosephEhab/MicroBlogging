using System.Linq.Expressions;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MicroBlogging.Persistence.Repositories;

public class GenericRepository<T>(MicroBloggingDbContext context) : IRepository<T> where T : BaseEntity
{
    public virtual async Task<T?> GetById(Guid id) => await context.Set<T>().FindAsync(id);

    public virtual async Task<List<T>?> GetByIds(List<Guid> guids) => await context.Set<T>().Where(e => guids.Contains(e.Id)).ToListAsync();

    public virtual async Task<List<T>> GetAll() => await context.Set<T>().ToListAsync();

    public virtual async Task<List<T>> GetNextN(Expression<Func<T, bool>> predicate, int n) => await context.Set<T>().OrderByDescending(e => e.CreatedAt).Where(predicate).Take(n).ToListAsync();

    public virtual async Task<List<T>> GetByCondition(Expression<Func<T, bool>> predicate) => await context.Set<T>().Where(predicate).ToListAsync();

    public virtual async Task<T?> FirstOrDefault(Expression<Func<T, bool>> predicate) => await context.Set<T>().FirstOrDefaultAsync(predicate);

    public virtual async Task Add(T entity) => await context.Set<T>().AddAsync(entity);

    public virtual Task Update(T entity)
    {
        context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task Delete(T entity)
    {
        context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task SaveChanges() => await context.SaveChangesAsync();
}