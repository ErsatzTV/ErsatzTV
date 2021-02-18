using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionShowConfiguration : IEntityTypeConfiguration<TelevisionShow>
    {
        public void Configure(EntityTypeBuilder<TelevisionShow> builder)
        {
            builder.ToTable("TelevisionShows");

            builder.HasOne(show => show.Metadata)
                .WithOne(metadata => metadata.TelevisionShow)
                .HasForeignKey<TelevisionShowMetadata>(metadata => metadata.TelevisionShowId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(show => show.Seasons)
                .WithOne(season => season.TelevisionShow);
        }
    }
}
