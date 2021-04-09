using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
    {
        public void Configure(EntityTypeBuilder<Artist> builder)
        {
            builder.ToTable("Artist");

            builder.HasMany(a => a.MusicVideos)
                .WithOne(mv => mv.Artist)
                .HasForeignKey(mv => mv.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.ArtistMetadata)
                .WithOne(am => am.Artist)
                .HasForeignKey(am => am.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
