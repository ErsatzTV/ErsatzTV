using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaSourceConfiguration : IEntityTypeConfiguration<MediaSource>
    {
        public void Configure(EntityTypeBuilder<MediaSource> builder) =>
            builder.HasIndex(ms => ms.Name).IsUnique();
    }
}
