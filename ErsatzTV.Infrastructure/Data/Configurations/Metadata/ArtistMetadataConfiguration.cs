using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ArtistMetadataConfiguration : IEntityTypeConfiguration<ArtistMetadata>
{
    public void Configure(EntityTypeBuilder<ArtistMetadata> builder)
    {
        builder.ToTable("ArtistMetadata");

        builder.HasMany(sm => sm.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Genres)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Styles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Moods)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}