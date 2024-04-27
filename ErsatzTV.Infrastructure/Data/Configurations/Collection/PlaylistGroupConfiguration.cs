using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlaylistGroupConfiguration : IEntityTypeConfiguration<PlaylistGroup>
{
    public void Configure(EntityTypeBuilder<PlaylistGroup> builder)
    {
        builder.ToTable("PlaylistGroup");

        builder.HasIndex(pg => pg.Name)
            .IsUnique();

        builder.HasMany(pg => pg.Playlists)
            .WithOne(p => p.PlaylistGroup)
            .HasForeignKey(p => p.PlaylistGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
