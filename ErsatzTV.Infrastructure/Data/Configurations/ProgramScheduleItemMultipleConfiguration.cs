using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ProgramScheduleItemMultipleConfiguration : IEntityTypeConfiguration<ProgramScheduleItemMultiple>
{
    public void Configure(EntityTypeBuilder<ProgramScheduleItemMultiple> builder) =>
        builder.ToTable("ProgramScheduleMultipleItem");
}
