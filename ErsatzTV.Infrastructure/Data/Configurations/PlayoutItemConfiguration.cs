using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlayoutItemConfiguration : IEntityTypeConfiguration<PlayoutItem>

{
    public void Configure(EntityTypeBuilder<PlayoutItem> builder)
    {
        builder.ToTable("PlayoutItem");

        builder.HasOne(pi => pi.MediaItem)
            .WithMany()
            .HasForeignKey(pi => pi.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Watermarks)
            .WithMany(m => m.PlayoutItems)
            .UsingEntity<PlayoutItemWatermark>(
                j => j.HasOne(ci => ci.Watermark)
                    .WithMany(mi => mi.PlayoutItemWatermarks)
                    .HasForeignKey(ci => ci.WatermarkId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.PlayoutItem)
                    .WithMany(c => c.PlayoutItemWatermarks)
                    .HasForeignKey(ci => ci.PlayoutItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.PlayoutItemId, ci.WatermarkId }));
    }
}
