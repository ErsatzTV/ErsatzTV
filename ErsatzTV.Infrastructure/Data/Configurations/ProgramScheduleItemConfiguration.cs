using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleItemConfiguration : IEntityTypeConfiguration<ProgramScheduleItem>
    {
        public void Configure(EntityTypeBuilder<ProgramScheduleItem> builder) =>
            builder.ToTable("ProgramScheduleItems");
    }
}
