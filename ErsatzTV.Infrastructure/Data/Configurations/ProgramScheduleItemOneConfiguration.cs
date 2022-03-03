using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ProgramScheduleItemOneConfiguration : IEntityTypeConfiguration<ProgramScheduleItemOne>
{
    public void Configure(EntityTypeBuilder<ProgramScheduleItemOne> builder) =>
        builder.ToTable("ProgramScheduleOneItem");
}