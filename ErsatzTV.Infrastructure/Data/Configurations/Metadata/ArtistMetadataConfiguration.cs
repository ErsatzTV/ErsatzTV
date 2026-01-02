using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ArtistMetadataConfiguration : IEntityTypeConfiguration<ArtistMetadata>
{
    public void Configure(EntityTypeBuilder<ArtistMetadata> builder)
    {
        builder.ToTable("ArtistMetadata");

        builder.HasIndex(am => am.Title);

        builder.HasMany(am => am.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Genres)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Tags)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Studios)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Actors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Guids)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Subtitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Styles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(am => am.Moods)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
