using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movie");

        builder.HasMany(m => m.MovieMetadata)
            .WithOne(m => m.Movie)
            .HasForeignKey(m => m.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.MediaVersions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}