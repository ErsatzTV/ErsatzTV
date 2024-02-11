using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Image");

        builder.HasMany(i => i.ImageMetadata)
            .WithOne(m => m.Image)
            .HasForeignKey(m => m.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.MediaVersions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
