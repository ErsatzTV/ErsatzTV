using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MovieMediaItemConfiguration : IEntityTypeConfiguration<MovieMediaItem>
    {
        public void Configure(EntityTypeBuilder<MovieMediaItem> builder)
        {
            builder.ToTable("Movies");

            builder.HasOne(i => i.Metadata)
                .WithOne(m => m.Movie)
                .HasForeignKey<MovieMetadata>(m => m.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
