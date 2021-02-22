using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class SimpleMediaCollectionConfiguration : IEntityTypeConfiguration<SimpleMediaCollection>
    {
        public void Configure(EntityTypeBuilder<SimpleMediaCollection> builder)
        {
            builder.ToTable("SimpleMediaCollections");

            builder.HasMany(c => c.Movies)
                .WithMany(m => m.SimpleMediaCollections)
                .UsingEntity(join => join.ToTable("SimpleMediaCollectionMovies"));

            builder.HasMany(c => c.TelevisionShows)
                .WithMany(s => s.SimpleMediaCollections)
                .UsingEntity(join => join.ToTable("SimpleMediaCollectionShows"));

            builder.HasMany(c => c.TelevisionSeasons)
                .WithMany(s => s.SimpleMediaCollections)
                .UsingEntity(join => join.ToTable("SimpleMediaCollectionSeasons"));

            builder.HasMany(c => c.TelevisionEpisodes)
                .WithMany(e => e.SimpleMediaCollections)
                .UsingEntity(join => join.ToTable("SimpleMediaCollectionEpisodes"));
        }
    }
}
