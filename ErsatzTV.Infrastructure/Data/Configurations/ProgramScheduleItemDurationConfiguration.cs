using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleItemDurationConfiguration : IEntityTypeConfiguration<ProgramScheduleItemDuration>
    {
        public void Configure(EntityTypeBuilder<ProgramScheduleItemDuration> builder)
        {
            builder.ToTable("ProgramScheduleDurationItem");
            
            builder.HasOne(i => i.TailCollection)
                .WithMany()
                .HasForeignKey(i => i.TailCollectionId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(i => i.TailMediaItem)
                .WithMany()
                .HasForeignKey(i => i.TailMediaItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(i => i.TailMultiCollection)
                .WithMany()
                .HasForeignKey(i => i.TailMultiCollectionId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}
