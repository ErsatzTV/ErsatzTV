using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ProgramScheduleItemConfiguration : IEntityTypeConfiguration<ProgramScheduleItem>
{
    public void Configure(EntityTypeBuilder<ProgramScheduleItem> builder)
    {
        builder.ToTable("ProgramScheduleItem");

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

        builder.HasOne(i => i.Playlist)
            .WithMany()
            .HasForeignKey(i => i.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.PreRollFiller)
            .WithMany()
            .HasForeignKey(i => i.PreRollFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.MidRollFiller)
            .WithMany()
            .HasForeignKey(i => i.MidRollFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.PostRollFiller)
            .WithMany()
            .HasForeignKey(i => i.PostRollFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.TailFiller)
            .WithMany()
            .HasForeignKey(i => i.TailFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.FallbackFiller)
            .WithMany()
            .HasForeignKey(i => i.FallbackFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(c => c.Watermarks)
            .WithMany(m => m.ProgramScheduleItems)
            .UsingEntity<ProgramScheduleItemWatermark>(
                j => j.HasOne(ci => ci.Watermark)
                    .WithMany(mi => mi.ProgramScheduleItemWatermarks)
                    .HasForeignKey(ci => ci.WatermarkId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.ProgramScheduleItem)
                    .WithMany(c => c.ProgramScheduleItemWatermarks)
                    .HasForeignKey(ci => ci.ProgramScheduleItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.ProgramScheduleItemId, ci.WatermarkId }));

        builder.HasMany(c => c.GraphicsElements)
            .WithMany(m => m.ProgramScheduleItems)
            .UsingEntity<ProgramScheduleItemGraphicsElement>(
                j => j.HasOne(ci => ci.GraphicsElement)
                    .WithMany(mi => mi.ProgramScheduleItemGraphicsElements)
                    .HasForeignKey(ci => ci.GraphicsElementId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.ProgramScheduleItem)
                    .WithMany(c => c.ProgramScheduleItemGraphicsElements)
                    .HasForeignKey(ci => ci.ProgramScheduleItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.ProgramScheduleItemId, ci.GraphicsElementId }));
    }
}
