using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionEpisodeMediaItemConfiguration : IEntityTypeConfiguration<TelevisionEpisodeMediaItem>
    {
        public void Configure(EntityTypeBuilder<TelevisionEpisodeMediaItem> builder)
        {
            builder.ToTable("TelevisionEpisodes");

            builder.HasOne(i => i.Metadata)
                .WithOne(m => m.TelevisionEpisode)
                .HasForeignKey<TelevisionEpisodeMetadata>(m => m.TelevisionEpisodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
