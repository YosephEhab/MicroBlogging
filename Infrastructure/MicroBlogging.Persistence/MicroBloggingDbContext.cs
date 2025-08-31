using MicroBlogging.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroBlogging.Persistence;

public class MicroBloggingDbContext(DbContextOptions<MicroBloggingDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<ImageAttachment> ImageAttachments => Set<ImageAttachment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicroBloggingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}