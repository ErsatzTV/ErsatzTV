using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class SeasonMetadataConfiguration : IEntityTypeConfiguration<SeasonMetadata>
{
    public void Configure(EntityTypeBuilder<SeasonMetadata> builder)
    {
        builder.ToTable("SeasonMetadata");

        builder.HasMany(em => em.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mm => mm.Guids)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}