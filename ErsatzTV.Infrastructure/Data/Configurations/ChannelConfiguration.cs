using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channel");

        builder.HasIndex(c => c.Number)
            .IsUnique();

        builder.HasMany(c => c.Playouts) // TODO: is this correct, or should we have one to one?
            .WithOne(p => p.Channel)
            .HasForeignKey(p => p.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Artwork)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Watermark)
            .WithMany()
            .HasForeignKey(i => i.WatermarkId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(i => i.FallbackFiller)
            .WithMany()
            .HasForeignKey(i => i.FallbackFillerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Property(c => c.Group)
            .IsRequired()
            .HasDefaultValue("ErsatzTV");
    }
}