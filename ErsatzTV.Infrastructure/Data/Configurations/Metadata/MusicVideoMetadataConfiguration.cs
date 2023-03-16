using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MusicVideoMetadataConfiguration : IEntityTypeConfiguration<MusicVideoMetadata>
{
    public void Configure(EntityTypeBuilder<MusicVideoMetadata> builder)
    {
        builder.ToTable("MusicVideoMetadata");

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

        builder.HasMany(mvm => mvm.Artists)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mvm => mvm.Directors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
