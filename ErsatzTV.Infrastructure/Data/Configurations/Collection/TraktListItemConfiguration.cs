using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class TraktListItemConfiguration : IEntityTypeConfiguration<TraktListItem>
{
    public void Configure(EntityTypeBuilder<TraktListItem> builder)
    {
        builder.ToTable("TraktListItem");

        builder.HasOne(i => i.MediaItem)
            .WithMany(mi => mi.TraktListItems)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Guids)
            .WithOne(g => g.TraktListItem)
            .HasForeignKey(g => g.TraktListItemId);
    }
}