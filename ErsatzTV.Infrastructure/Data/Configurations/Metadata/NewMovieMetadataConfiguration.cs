using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class NewMovieMetadataConfiguration : IEntityTypeConfiguration<NewMovieMetadata>
    {
        public void Configure(EntityTypeBuilder<NewMovieMetadata> builder) => builder.ToTable("NewMovieMetadata");
    }
}
