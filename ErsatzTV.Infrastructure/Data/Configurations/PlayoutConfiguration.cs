using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlayoutConfiguration : IEntityTypeConfiguration<Playout>
    {
        public void Configure(EntityTypeBuilder<Playout> builder)
        {
            builder.HasMany(p => p.Items)
                .WithOne(pi => pi.Playout)
                .HasForeignKey(pi => pi.PlayoutId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(c => c.Anchor);

            builder.HasMany(p => p.ProgramScheduleAnchors)
                .WithOne(a => a.Playout)
                .HasForeignKey(a => a.PlayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
