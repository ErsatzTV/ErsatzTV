using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MediaVersionConfiguration : IEntityTypeConfiguration<MediaVersion>
{
    public void Configure(EntityTypeBuilder<MediaVersion> builder)
    {
        builder.ToTable("MediaVersion");

        builder.HasMany(v => v.MediaFiles)
            .WithOne(f => f.MediaVersion)
            .HasForeignKey(f => f.MediaVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Streams)
            .WithOne(s => s.MediaVersion)
            .HasForeignKey(s => s.MediaVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Chapters)
            .WithOne(c => c.MediaVersion)
            .HasForeignKey(c => c.MediaVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
