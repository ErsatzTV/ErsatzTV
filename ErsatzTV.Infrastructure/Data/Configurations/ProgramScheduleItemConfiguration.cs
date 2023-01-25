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

        builder.HasOne(i => i.PreRollFiller)
            .WithMany()
            .HasForeignKey(i => i.PreRollFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.MidRollEnterFiller)
            .WithMany()
            .HasForeignKey(i => i.MidRollEnterFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.MidRollFiller)
            .WithMany()
            .HasForeignKey(i => i.MidRollFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.MidRollExitFiller)
            .WithMany()
            .HasForeignKey(i => i.MidRollExitFillerId)
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

        builder.HasOne(i => i.Watermark)
            .WithMany()
            .HasForeignKey(i => i.WatermarkId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
