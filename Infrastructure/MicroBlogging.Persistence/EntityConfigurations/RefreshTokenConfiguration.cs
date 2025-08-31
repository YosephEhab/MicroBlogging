using MicroBlogging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroBlogging.Persistence.EntityConfigurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");

        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(512);

        b.Property(x => x.ExpiresAt).IsRequired();
        b.Property(x => x.IsRevoked).HasDefaultValue(false);

        b.Property(x => x.CreatedAt).IsRequired();

        b.Property(x => x.CreatedByIp)
            .HasMaxLength(64);

        b.Property(x => x.ReplacedByToken)
            .HasMaxLength(512);

        b.HasIndex(x => new { x.UserId, x.Token }).IsUnique();
        b.HasIndex(x => x.ExpiresAt);
    }
}