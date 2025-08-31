using MicroBlogging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroBlogging.Persistence.EntityConfigurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("Posts");

        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(140);

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);

        // Timeline-friendly indexes
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.UserId);

        // Value object: GeoLocation stored inline
        b.OwnsOne(x => x.Location, loc =>
        {
            loc.Property(p => p.Latitude)
               .HasPrecision(9, 6)
               .HasColumnName("Latitude")
               .IsRequired();

            loc.Property(p => p.Longitude)
               .HasPrecision(9, 6)
               .HasColumnName("Longitude")
               .IsRequired();

            loc.WithOwner();
        });

        // One Post -> many ImageAttachments
        b.HasMany(x => x.Images)
         .WithOne()              // no navigation on ImageAttachment back to Post
         .HasForeignKey(i => i.PostId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}