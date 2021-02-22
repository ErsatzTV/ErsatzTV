using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionShowMetadataConfiguration : IEntityTypeConfiguration<TelevisionShowMetadata>
    {
        public void Configure(EntityTypeBuilder<TelevisionShowMetadata> builder) =>
            builder.ToTable("TelevisionShowMetadata");
    }
}
