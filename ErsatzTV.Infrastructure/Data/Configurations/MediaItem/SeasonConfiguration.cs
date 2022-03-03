using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Season");

        builder.HasMany(s => s.Episodes)
            .WithOne(e => e.Season)
            .HasForeignKey(e => e.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.SeasonMetadata)
            .WithOne(s => s.Season)
            .HasForeignKey(s => s.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}