using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Song");

        builder.HasMany(m => m.SongMetadata)
            .WithOne(m => m.Song)
            .HasForeignKey(m => m.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.MediaVersions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}