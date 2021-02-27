using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EpisodeMetadataConfiguration : IEntityTypeConfiguration<EpisodeMetadata>
    {
        public void Configure(EntityTypeBuilder<EpisodeMetadata> builder)
        {
            builder.ToTable("EpisodeMetadata");

            builder.HasMany(em => em.Artwork)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
