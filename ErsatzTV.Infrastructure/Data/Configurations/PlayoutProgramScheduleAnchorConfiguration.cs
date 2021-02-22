using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlayoutProgramScheduleAnchorConfiguration : IEntityTypeConfiguration<PlayoutProgramScheduleAnchor>
    {
        public void Configure(EntityTypeBuilder<PlayoutProgramScheduleAnchor> builder) =>
            builder.OwnsOne(a => a.EnumeratorState)
                .WithOwner();
    }
}
