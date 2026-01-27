using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class FFmpegProfileConfiguration : IEntityTypeConfiguration<FFmpegProfile>
{
    public void Configure(EntityTypeBuilder<FFmpegProfile> builder)
    {
        builder.ToTable("FFmpegProfile");

        builder.Property(p => p.NormalizeFramerate)
            .HasDefaultValue(false);

        builder.Property(p => p.DeinterlaceVideo)
            .HasDefaultValue(true);

        builder.Property(p => p.NormalizeAudio)
            .HasDefaultValue(true);

        builder.Property(p => p.NormalizeVideo)
            .HasDefaultValue(true);

        builder.Property(p => p.NormalizeColors)
            .HasDefaultValue(true);
    }
}
