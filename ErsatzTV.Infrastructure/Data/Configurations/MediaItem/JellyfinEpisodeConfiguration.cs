using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinEpisodeConfiguration : IEntityTypeConfiguration<JellyfinEpisode>
{
    public void Configure(EntityTypeBuilder<JellyfinEpisode> builder)
    {
        builder.ToTable("JellyfinEpisode");

        builder.Property(e => e.Etag)
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.Property(e => e.ItemId)
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.HasIndex(e => e.ItemId);
    }
}
