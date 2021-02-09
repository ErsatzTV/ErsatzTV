using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaCollectionConfiguration : IEntityTypeConfiguration<MediaCollection>
    {
        public void Configure(EntityTypeBuilder<MediaCollection> builder) =>
            builder.HasIndex(c => c.Name).IsUnique();
    }
}
