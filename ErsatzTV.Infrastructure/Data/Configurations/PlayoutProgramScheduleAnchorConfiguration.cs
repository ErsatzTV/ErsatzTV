using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlayoutProgramScheduleAnchorConfiguration : IEntityTypeConfiguration<PlayoutProgramScheduleAnchor>
    {
        public void Configure(EntityTypeBuilder<PlayoutProgramScheduleAnchor> builder)
        {
            builder.ToTable("PlayoutProgramScheduleAnchor");

            builder.OwnsOne(a => a.EnumeratorState)
                .WithOwner();

            // TODO: fix this foreign key
            builder.HasOne(i => i.Collection)
                .WithMany()
                .HasForeignKey(i => i.NewCollectionId)
                .IsRequired(false);

            builder.HasOne(i => i.MediaItem)
                .WithMany()
                .HasForeignKey(i => i.MediaItemId)
                .IsRequired(false);
        }
    }
}
