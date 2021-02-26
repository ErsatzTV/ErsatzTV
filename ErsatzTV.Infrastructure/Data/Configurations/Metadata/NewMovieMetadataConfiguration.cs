using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class NewMovieMetadataConfiguration : IEntityTypeConfiguration<MovieMetadata>
    {
        public void Configure(EntityTypeBuilder<MovieMetadata> builder) => builder.ToTable("NewMovieMetadata");
    }
}
