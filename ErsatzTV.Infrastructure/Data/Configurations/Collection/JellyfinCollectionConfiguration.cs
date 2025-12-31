using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinCollectionConfiguration : IEntityTypeConfiguration<JellyfinCollection>
{
    public void Configure(EntityTypeBuilder<JellyfinCollection> builder)
    {
        builder.ToTable("JellyfinCollection");

        builder.Property(c => c.Etag)
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.Property(c => c.ItemId)
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.HasIndex(c => c.ItemId);
    }
}
