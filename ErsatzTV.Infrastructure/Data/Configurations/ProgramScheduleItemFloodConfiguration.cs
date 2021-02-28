using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleItemFloodConfiguration : IEntityTypeConfiguration<ProgramScheduleItemFlood>
    {
        public void Configure(EntityTypeBuilder<ProgramScheduleItemFlood> builder) =>
            builder.ToTable("ProgramScheduleFloodItem");
    }
}
