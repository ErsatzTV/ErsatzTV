using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class RerunHistoryConfiguration : IEntityTypeConfiguration<RerunHistory>
{
    public void Configure(EntityTypeBuilder<RerunHistory> builder)
    {
        builder.ToTable("RerunHistory");

        builder.HasOne(p => p.Playout)
            .WithMany()
            .HasForeignKey(pi => pi.PlayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.RerunCollection)
            .WithMany()
            .HasForeignKey(pi => pi.RerunCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.MediaItem)
            .WithMany()
            .HasForeignKey(pi => pi.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
