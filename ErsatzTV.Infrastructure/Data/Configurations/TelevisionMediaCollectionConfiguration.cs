using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionMediaCollectionConfiguration : IEntityTypeConfiguration<TelevisionMediaCollection>
    {
        public void Configure(EntityTypeBuilder<TelevisionMediaCollection> builder)
        {
            builder.ToTable("TelevisionMediaCollections");

            builder.HasIndex(c => new { c.ShowTitle, c.SeasonNumber })
                .IsUnique();
        }
    }
}
