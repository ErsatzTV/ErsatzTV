using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ProgramScheduleAlternateConfiguration : IEntityTypeConfiguration<ProgramScheduleAlternate>
{
    public void Configure(EntityTypeBuilder<ProgramScheduleAlternate> builder)
    {
        builder.ToTable("ProgramScheduleAlternate");

        builder.Property(t => t.DaysOfMonth)
            .HasConversion<IntCollectionValueConverter, CollectionValueComparer<int>>();

        builder.Property(t => t.MonthsOfYear)
            .HasConversion<IntCollectionValueConverter, CollectionValueComparer<int>>();

        builder.Property(t => t.DaysOfWeek)
            .HasConversion<EnumCollectionJsonValueConverter<DayOfWeek>, CollectionValueComparer<DayOfWeek>>();
    }
}
