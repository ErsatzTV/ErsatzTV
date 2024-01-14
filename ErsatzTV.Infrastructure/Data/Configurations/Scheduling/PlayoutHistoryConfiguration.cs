using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class PlayoutHistoryConfiguration : IEntityTypeConfiguration<PlayoutHistory>
{
    public void Configure(EntityTypeBuilder<PlayoutHistory> builder)
    {
        builder.ToTable("PlayoutHistory");
    }
}
