using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleItemDurationConfiguration : IEntityTypeConfiguration<ProgramScheduleItemDuration>
    {
        public void Configure(EntityTypeBuilder<ProgramScheduleItemDuration> builder) =>
            builder.ToTable("ProgramScheduleDurationItems");
    }
}
