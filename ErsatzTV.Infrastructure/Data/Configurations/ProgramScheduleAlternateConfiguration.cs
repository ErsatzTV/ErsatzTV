using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ProgramScheduleAlternateConfiguration : IEntityTypeConfiguration<ProgramScheduleAlternate>
{
    public void Configure(EntityTypeBuilder<ProgramScheduleAlternate> builder)
    {
        builder.ToTable("ProgramScheduleAlternate");

        var intCollectionValueConverter = new ValueConverter<ICollection<int>, string>(
            i => string.Join(",", i),
            s => string.IsNullOrWhiteSpace(s)
                ? Array.Empty<int>()
                : s.Split(new[] { ',' }).Select(int.Parse).ToArray());

        var intCollectionValueComparer = new CollectionValueComparer<int>();

        builder.Property(t => t.DaysOfMonth)
            .HasConversion(intCollectionValueConverter)
            .Metadata.SetValueComparer(intCollectionValueComparer);

        builder.Property(t => t.MonthsOfYear)
            .HasConversion(intCollectionValueConverter)
            .Metadata.SetValueComparer(intCollectionValueComparer);

        builder.Property(t => t.DaysOfWeek)
            .HasConversion(new EnumCollectionJsonValueConverter<DayOfWeek>())
            .Metadata.SetValueComparer(new CollectionValueComparer<DayOfWeek>());
    }
}
