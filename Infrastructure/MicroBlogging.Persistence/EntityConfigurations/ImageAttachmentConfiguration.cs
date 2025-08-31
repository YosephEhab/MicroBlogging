using MicroBlogging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroBlogging.Persistence.EntityConfigurations;

public sealed class ImageAttachmentConfiguration : IEntityTypeConfiguration<ImageAttachment>
{
    public void Configure(EntityTypeBuilder<ImageAttachment> b)
    {
        b.ToTable("ImageAttachments");

        b.HasKey(x => x.Id);

        b.Property(x => x.PostId).IsRequired();

        b.Property(x => x.OriginalUrl)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);

        b.HasIndex(x => x.PostId);

        // Owned collection: ImageVariants
        b.OwnsMany<ImageVariant>("Variants", v =>
        {
            v.ToTable("ImageVariants");

            // Shadow PK for the owned VO
            v.Property<int>("Id");
            v.HasKey("Id");

            // FK back to ImageAttachment
            v.WithOwner().HasForeignKey("ImageAttachmentId");
            v.HasIndex("ImageAttachmentId");

            v.Property(p => p.Url)
                .IsRequired()
                .HasMaxLength(500);

            v.Property(p => p.Width).IsRequired();
            v.Property(p => p.Height).IsRequired();

            v.Property(p => p.Format)
                .IsRequired()
                .HasMaxLength(16);
        });
    }
}