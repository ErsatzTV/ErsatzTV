using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MusicVideoConfiguration : IEntityTypeConfiguration<MusicVideo>
{
    public void Configure(EntityTypeBuilder<MusicVideo> builder)
    {
        builder.ToTable("MusicVideo");

        builder.HasMany(m => m.MusicVideoMetadata)
            .WithOne(m => m.MusicVideo)
            .HasForeignKey(m => m.MusicVideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.MediaVersions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}