using ErsatzTV.Core.AggregateModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaItemSummaryConfiguration : IEntityTypeConfiguration<MediaItemSummary>
    {
        public void Configure(EntityTypeBuilder<MediaItemSummary> builder) =>
            builder.HasNoKey().ToView(null);
    }
}
