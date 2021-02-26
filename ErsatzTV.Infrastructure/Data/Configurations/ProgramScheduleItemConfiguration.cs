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

            builder.HasOne(i => i.MediaCollection)
                .WithMany()
                .HasForeignKey(i => i.MediaCollectionId)
                .IsRequired(false);

            builder.HasOne(i => i.TelevisionShow)
                .WithMany()
                .HasForeignKey(i => i.TelevisionShowId)
                .IsRequired(false);

            builder.HasOne(i => i.TelevisionSeason)
                .WithMany()
                .HasForeignKey(i => i.TelevisionSeasonId)
                .IsRequired(false);

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
