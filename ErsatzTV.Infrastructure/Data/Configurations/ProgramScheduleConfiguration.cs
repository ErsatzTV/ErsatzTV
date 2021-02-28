using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleConfiguration : IEntityTypeConfiguration<ProgramSchedule>
    {
        public void Configure(EntityTypeBuilder<ProgramSchedule> builder)
        {
            builder.ToTable("ProgramSchedule");

            builder.HasIndex(ps => ps.Name)
                .IsUnique();

            builder.HasMany(ps => ps.Items)
                .WithOne(i => i.ProgramSchedule)
                .HasForeignKey(i => i.ProgramScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(ps => ps.Playouts)
                .WithOne(p => p.ProgramSchedule)
                .HasForeignKey(p => p.ProgramScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
