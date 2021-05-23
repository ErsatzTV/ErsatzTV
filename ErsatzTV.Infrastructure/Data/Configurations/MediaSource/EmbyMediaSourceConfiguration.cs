using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EmbyMediaSourceConfiguration : IEntityTypeConfiguration<EmbyMediaSource>
    {
        public void Configure(EntityTypeBuilder<EmbyMediaSource> builder)
        {
            builder.ToTable("EmbyMediaSource");

            builder.HasMany(s => s.Connections)
                .WithOne(c => c.EmbyMediaSource)
                .HasForeignKey(c => c.EmbyMediaSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.PathReplacements)
                .WithOne(r => r.EmbyMediaSource)
                .HasForeignKey(r => r.EmbyMediaSourceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
