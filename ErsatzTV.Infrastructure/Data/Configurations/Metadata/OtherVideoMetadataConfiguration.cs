using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class OtherVideoMetadataConfiguration : IEntityTypeConfiguration<OtherVideoMetadata>
{
    public void Configure(EntityTypeBuilder<OtherVideoMetadata> builder)
    {
        builder.ToTable("OtherVideoMetadata");

        builder.HasMany(mm => mm.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Tags)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Subtitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
