using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
{
    public void Configure(EntityTypeBuilder<PlaylistItem> builder)
    {
        builder.ToTable("PlaylistItem");
        
        builder.HasOne(i => i.Collection)
            .WithMany()
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MediaItem)
            .WithMany()
            .HasForeignKey(i => i.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MultiCollection)
            .WithMany()
            .HasForeignKey(i => i.MultiCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.SmartCollection)
            .WithMany()
            .HasForeignKey(i => i.SmartCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
