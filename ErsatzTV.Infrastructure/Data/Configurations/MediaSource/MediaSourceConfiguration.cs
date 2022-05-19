using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MediaSourceConfiguration : IEntityTypeConfiguration<MediaSource>
{
    public void Configure(EntityTypeBuilder<MediaSource> builder)
    {
        builder.ToTable("MediaSource");

        builder.HasMany(s => s.Libraries)
            .WithOne(l => l.MediaSource)
            .HasForeignKey(l => l.MediaSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
