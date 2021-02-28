using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
    {
        public void Configure(EntityTypeBuilder<Episode> builder)
        {
            builder.ToTable("Episode");

            builder.HasMany(e => e.EpisodeMetadata)
                .WithOne(m => m.Episode)
                .HasForeignKey(m => m.EpisodeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.MediaVersions)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
