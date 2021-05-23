using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EmbyEpisodeConfiguration : IEntityTypeConfiguration<EmbyEpisode>
    {
        public void Configure(EntityTypeBuilder<EmbyEpisode> builder) => builder.ToTable("EmbyEpisode");
    }
}
