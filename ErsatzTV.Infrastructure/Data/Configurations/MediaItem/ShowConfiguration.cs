using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder.ToTable("Show");

        builder.HasMany(s => s.Seasons)
            .WithOne(s => s.Show)
            .HasForeignKey(s => s.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ShowMetadata)
            .WithOne(s => s.Show)
            .HasForeignKey(s => s.ShowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}