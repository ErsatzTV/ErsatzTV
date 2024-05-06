using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("Playlist");

        builder.HasIndex(d => new { d.PlaylistGroupId, d.Name })
            .IsUnique();

        builder.HasMany(p => p.Items)
            .WithOne(pi => pi.Playlist)
            .HasForeignKey(pi => pi.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
