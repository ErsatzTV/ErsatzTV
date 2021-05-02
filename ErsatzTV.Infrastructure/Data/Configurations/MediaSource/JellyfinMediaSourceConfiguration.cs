using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class JellyfinMediaSourceConfiguration : IEntityTypeConfiguration<JellyfinMediaSource>
    {
        public void Configure(EntityTypeBuilder<JellyfinMediaSource> builder)
        {
            builder.ToTable("JellyfinMediaSource");

            builder.HasMany(s => s.Connections)
                .WithOne(c => c.JellyfinMediaSource)
                .HasForeignKey(c => c.JellyfinMediaSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.PathReplacements)
                .WithOne(r => r.JellyfinMediaSource)
                .HasForeignKey(r => r.JellyfinMediaSourceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
