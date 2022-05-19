using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexMediaSourceConfiguration : IEntityTypeConfiguration<PlexMediaSource>
{
    public void Configure(EntityTypeBuilder<PlexMediaSource> builder)
    {
        builder.ToTable("PlexMediaSource");

        builder.HasMany(s => s.Connections)
            .WithOne(c => c.PlexMediaSource)
            .HasForeignKey(c => c.PlexMediaSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.PathReplacements)
            .WithOne(r => r.PlexMediaSource)
            .HasForeignKey(r => r.PlexMediaSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
