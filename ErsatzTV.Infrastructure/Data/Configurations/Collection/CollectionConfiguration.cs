using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("Collection");

        builder.Property(c => c.Name)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasMany(c => c.MediaItems)
            .WithMany(m => m.Collections)
            .UsingEntity<CollectionItem>(
                j => j.HasOne(ci => ci.MediaItem)
                    .WithMany(mi => mi.CollectionItems)
                    .HasForeignKey(ci => ci.MediaItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(ci => ci.Collection)
                    .WithMany(c => c.CollectionItems)
                    .HasForeignKey(ci => ci.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(ci => new { ci.CollectionId, ci.MediaItemId }));
    }
}
