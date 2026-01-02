using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ChannelWatermarkConfiguration : IEntityTypeConfiguration<ChannelWatermark>
{
    public void Configure(EntityTypeBuilder<ChannelWatermark> builder)
    {
        builder.ToTable("ChannelWatermark");

        builder.Property(wm => wm.Name)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(wm => wm.Name)
            .IsUnique();
    }
}
