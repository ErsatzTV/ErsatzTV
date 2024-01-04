using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class
    PlayoutScheduleItemFillGroupIndexConfiguration : IEntityTypeConfiguration<PlayoutScheduleItemFillGroupIndex>
{
    public void Configure(EntityTypeBuilder<PlayoutScheduleItemFillGroupIndex> builder)
    {
        builder.ToTable("PlayoutScheduleItemFillGroupIndex");

        builder.OwnsOne(a => a.EnumeratorState).ToTable("FillGroupEnumeratorState");

        builder.HasOne(i => i.ProgramScheduleItem)
            .WithMany()
            .HasForeignKey(i => i.ProgramScheduleItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
