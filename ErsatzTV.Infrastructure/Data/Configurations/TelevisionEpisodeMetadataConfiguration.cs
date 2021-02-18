using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionEpisodeMetadataConfiguration : IEntityTypeConfiguration<TelevisionEpisodeMetadata>
    {
        public void Configure(EntityTypeBuilder<TelevisionEpisodeMetadata> builder) =>
            builder.ToTable("TelevisionEpisodeMetadata");
    }
}
