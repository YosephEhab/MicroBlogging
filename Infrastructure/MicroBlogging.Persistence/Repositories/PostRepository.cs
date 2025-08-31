using System.Linq.Expressions;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MicroBlogging.Persistence.Repositories;

public class PostRepository(MicroBloggingDbContext dbContext) : GenericRepository<Post>(dbContext), IPostRepository
{
    public override async Task<List<Post>> GetNextN(Expression<Func<Post, bool>> predicate, int n)
    {
        return await dbContext.Posts
            .Include(p => p.Images)
                .ThenInclude(i => i.Variants)
            .Where(predicate)
            .OrderByDescending(p => p.CreatedAt)
            .Take(n)
            .ToListAsync();
    }
}
