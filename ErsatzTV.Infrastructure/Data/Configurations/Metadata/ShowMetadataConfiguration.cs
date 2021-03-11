using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ShowMetadataConfiguration : IEntityTypeConfiguration<ShowMetadata>
    {
        public void Configure(EntityTypeBuilder<ShowMetadata> builder)
        {
            builder.ToTable("ShowMetadata");

            builder.HasMany(sm => sm.Artwork)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(mm => mm.Genres)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(mm => mm.Tags)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
