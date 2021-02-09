using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class SimpleMediaCollectionConfiguration : IEntityTypeConfiguration<SimpleMediaCollection>
    {
        public void Configure(EntityTypeBuilder<SimpleMediaCollection> builder)
        {
            builder.ToTable("SimpleMediaCollections");

            builder.HasMany(cg => cg.Items)
                .WithMany(c => c.SimpleMediaCollections);
        }
    }
}
