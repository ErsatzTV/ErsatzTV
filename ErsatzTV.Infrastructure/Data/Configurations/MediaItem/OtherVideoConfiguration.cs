using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class OtherVideoConfiguration : IEntityTypeConfiguration<OtherVideo>
    {
        public void Configure(EntityTypeBuilder<OtherVideo> builder)
        {
            builder.ToTable("OtherVideo");

            builder.HasMany(m => m.OtherVideoMetadata)
                .WithOne(m => m.OtherVideo)
                .HasForeignKey(m => m.OtherVideoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.MediaVersions)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
