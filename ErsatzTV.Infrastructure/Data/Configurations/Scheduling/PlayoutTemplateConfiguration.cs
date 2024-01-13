using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class PlayoutTemplateConfiguration : IEntityTypeConfiguration<PlayoutTemplate>
{
    public void Configure(EntityTypeBuilder<PlayoutTemplate> builder)
    {
        builder.ToTable("PlayoutTemplate");
        
        builder.Property(t => t.DaysOfMonth)
            .HasConversion<IntCollectionValueConverter, CollectionValueComparer<int>>();

        builder.Property(t => t.MonthsOfYear)
            .HasConversion<IntCollectionValueConverter, CollectionValueComparer<int>>();

        builder.Property(t => t.DaysOfWeek)
            .HasConversion<EnumCollectionJsonValueConverter<DayOfWeek>, CollectionValueComparer<DayOfWeek>>();
    }
}
