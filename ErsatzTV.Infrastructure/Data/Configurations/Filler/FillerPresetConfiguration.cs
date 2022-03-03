using ErsatzTV.Core.Domain.Filler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Filler;

public class FillerPresetConfiguration : IEntityTypeConfiguration<FillerPreset>
{
    public void Configure(EntityTypeBuilder<FillerPreset> builder)
    {
        builder.ToTable("FillerPreset");

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