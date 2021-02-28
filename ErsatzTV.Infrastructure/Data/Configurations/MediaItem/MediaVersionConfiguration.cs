using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaVersionConfiguration : IEntityTypeConfiguration<MediaVersion>
    {
        public void Configure(EntityTypeBuilder<MediaVersion> builder)
        {
            builder.ToTable("MediaVersion");

            builder.HasMany(v => v.MediaFiles)
                .WithOne(f => f.MediaVersion)
                .HasForeignKey(f => f.MediaVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
