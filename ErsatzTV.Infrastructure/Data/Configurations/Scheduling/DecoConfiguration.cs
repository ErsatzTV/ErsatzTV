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

        builder.HasOne(d => d.Watermark)
            .WithMany()
            .HasForeignKey(d => d.WatermarkId)
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
    }
}
