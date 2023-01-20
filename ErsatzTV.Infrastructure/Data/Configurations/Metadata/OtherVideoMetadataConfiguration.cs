using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class OtherVideoMetadataConfiguration : IEntityTypeConfiguration<OtherVideoMetadata>
{
    public void Configure(EntityTypeBuilder<OtherVideoMetadata> builder)
    {
        builder.ToTable("OtherVideoMetadata");

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
        
        builder.HasMany(ovm => ovm.Directors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Writers)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
