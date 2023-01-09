using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlayoutConfiguration : IEntityTypeConfiguration<Playout>
{
    public void Configure(EntityTypeBuilder<Playout> builder)
    {
        builder.ToTable("Playout");

        builder.HasMany(p => p.ProgramScheduleAlternates)
            .WithOne(a => a.Playout)
            .HasForeignKey(a => a.PlayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Items)
            .WithOne(pi => pi.Playout)
            .HasForeignKey(pi => pi.PlayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(p => p.Anchor)
            .ToTable("PlayoutAnchor")
            .OwnsOne(a => a.ScheduleItemsEnumeratorState)
            .ToTable("ScheduleItemsEnumeratorState");

        builder.HasMany(p => p.ProgramScheduleAnchors)
            .WithOne(a => a.Playout)
            .HasForeignKey(a => a.PlayoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
