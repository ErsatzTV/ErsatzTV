using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class SongMetadataConfiguration : IEntityTypeConfiguration<SongMetadata>
{
    public void Configure(EntityTypeBuilder<SongMetadata> builder)
    {
        builder.ToTable("SongMetadata");

        builder.HasMany(mm => mm.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Genres)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Tags)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Studios)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}