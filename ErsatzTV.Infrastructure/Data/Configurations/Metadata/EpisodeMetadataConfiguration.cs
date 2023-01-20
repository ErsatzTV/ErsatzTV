using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EpisodeMetadataConfiguration : IEntityTypeConfiguration<EpisodeMetadata>
{
    public void Configure(EntityTypeBuilder<EpisodeMetadata> builder)
    {
        builder.ToTable("EpisodeMetadata");

        builder.HasMany(sm => sm.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Genres)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Tags)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Studios)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sm => sm.Actors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Guids)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Subtitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(mm => mm.Directors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Writers)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
