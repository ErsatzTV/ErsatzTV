using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class ProgramScheduleItemConfiguration : IEntityTypeConfiguration<ProgramScheduleItem>
    {
        public void Configure(EntityTypeBuilder<ProgramScheduleItem> builder)
        {
            builder.ToTable("ProgramScheduleItem");

            builder.HasOne(i => i.Collection)
                .WithMany()
                .HasForeignKey(i => i.CollectionId)
                .IsRequired(false);

            builder.HasOne(i => i.MediaItem)
                .WithMany()
                .HasForeignKey(i => i.MediaItemId)
                .IsRequired(false);
        }
    }
}
