using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class OtherVideoMetadataConfiguration : IEntityTypeConfiguration<OtherVideoMetadata>
{
    public void Configure(EntityTypeBuilder<OtherVideoMetadata> builder)
    {
        builder.ToTable("OtherVideoMetadata");

        builder.HasMany(ovm => ovm.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Genres)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Tags)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Studios)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Actors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Directors)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Writers)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Guids)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ovm => ovm.Subtitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
