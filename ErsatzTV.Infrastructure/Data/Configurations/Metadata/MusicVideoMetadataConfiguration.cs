using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MusicVideoMetadataConfiguration : IEntityTypeConfiguration<MusicVideoMetadata>
{
    public void Configure(EntityTypeBuilder<MusicVideoMetadata> builder)
    {
        builder.ToTable("MusicVideoMetadata");

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
        
        builder.HasMany(mvm => mvm.Subtitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}