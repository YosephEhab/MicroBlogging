using MicroBlogging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroBlogging.Persistence.EntityConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");

        b.HasKey(x => x.Id);

        b.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(50);

        b.HasIndex(x => x.Username)
            .IsUnique();

        b.Property(x => x.PasswordHash)
            .IsRequired();

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
    }
}
