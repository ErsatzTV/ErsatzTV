using ErsatzTV.Core.AggregateModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaCollectionSummaryConfiguration : IEntityTypeConfiguration<MediaCollectionSummary>
    {
        public void Configure(EntityTypeBuilder<MediaCollectionSummary> builder) =>
            builder.HasNoKey().ToView(null);
    }
}
