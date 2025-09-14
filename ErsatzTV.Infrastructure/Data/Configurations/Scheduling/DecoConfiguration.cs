using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoConfiguration : IEntityTypeConfiguration<Deco>
{
    public void Configure(EntityTypeBuilder<Deco> builder)
    {
        builder.ToTable("Deco");

        builder.HasIndex(d => new { d.DecoGroupId, d.Name })
            .IsUnique();

        builder.HasMany(d => d.Playouts)
            .WithOne(p => p.Deco)
            .HasForeignKey(p => p.DecoId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(d => d.DeadAirFallbackCollection)
            .WithMany()
            .HasForeignKey(d => d.DeadAirFallbackCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(d => d.DeadAirFallbackMediaItem)
            .WithMany()
            .HasForeignKey(d => d.DeadAirFallbackMediaItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(d => d.DeadAirFallbackMultiCollection)
            .WithMany()
            .HasForeignKey(d => d.DeadAirFallbackMultiCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(d => d.DeadAirFallbackSmartCollection)
            .WithMany()
            .HasForeignKey(d => d.DeadAirFallbackSmartCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasMany(c => c.Watermarks)
            .WithMany(m => m.Decos)
            .UsingEntity<DecoWatermark>(
                j => j.HasOne(ci => ci.Watermark)
                    .WithMany(mi => mi.DecoWatermarks)
                    .HasForeignKey(ci => ci.WatermarkId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.Deco)
                    .WithMany(c => c.DecoWatermarks)
                    .HasForeignKey(ci => ci.DecoId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.DecoId, ci.WatermarkId }));

        builder.HasMany(c => c.GraphicsElements)
            .WithMany(m => m.Decos)
            .UsingEntity<DecoGraphicsElement>(
                j => j.HasOne(ci => ci.GraphicsElement)
                    .WithMany(mi => mi.DecoGraphicsElements)
                    .HasForeignKey(ci => ci.GraphicsElementId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.Deco)
                    .WithMany(c => c.DecoGraphicsElements)
                    .HasForeignKey(ci => ci.DecoId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.DecoId, ci.GraphicsElementId }));
    }
}
