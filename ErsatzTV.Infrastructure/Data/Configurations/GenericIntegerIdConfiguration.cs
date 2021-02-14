using ErsatzTV.Core.AggregateModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class GenericIntegerIdConfiguration : IEntityTypeConfiguration<GenericIntegerId>
    {
        public void Configure(EntityTypeBuilder<GenericIntegerId> builder) =>
            builder.HasNoKey().ToView(null);
    }
}
