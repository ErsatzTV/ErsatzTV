using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlayoutProgramScheduleAnchorConfiguration : IEntityTypeConfiguration<PlayoutProgramScheduleAnchor>
    {
        public void Configure(EntityTypeBuilder<PlayoutProgramScheduleAnchor> builder)
        {
            builder.HasKey(a => new { a.PlayoutId, a.ProgramScheduleId, ContentGroupId = a.MediaCollectionId });

            builder.OwnsOne(a => a.EnumeratorState);
        }
    }
}
