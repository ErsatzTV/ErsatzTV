using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class BlockItemConfiguration : IEntityTypeConfiguration<BlockItem>
{
    public void Configure(EntityTypeBuilder<BlockItem> builder)
    {
        builder.ToTable("BlockItem");

        builder.HasOne(i => i.Collection)
            .WithMany()
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MediaItem)
            .WithMany()
            .HasForeignKey(i => i.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MultiCollection)
            .WithMany()
            .HasForeignKey(i => i.MultiCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.SmartCollection)
            .WithMany()
            .HasForeignKey(i => i.SmartCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasMany(c => c.Watermarks)
            .WithMany(m => m.BlockItems)
            .UsingEntity<BlockItemWatermark>(
                j => j.HasOne(ci => ci.Watermark)
                    .WithMany(mi => mi.BlockItemWatermarks)
                    .HasForeignKey(ci => ci.WatermarkId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.BlockItem)
                    .WithMany(c => c.BlockItemWatermarks)
                    .HasForeignKey(ci => ci.BlockItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.BlockItemId, ci.WatermarkId }));

        builder.HasMany(c => c.GraphicsElements)
            .WithMany(m => m.BlockItems)
            .UsingEntity<BlockItemGraphicsElement>(
                j => j.HasOne(ci => ci.GraphicsElement)
                    .WithMany(mi => mi.BlockItemGraphicsElements)
                    .HasForeignKey(ci => ci.GraphicsElementId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.BlockItem)
                    .WithMany(c => c.BlockItemGraphicsElements)
                    .HasForeignKey(ci => ci.BlockItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.BlockItemId, ci.GraphicsElementId }));
    }
}
