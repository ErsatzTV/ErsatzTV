using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoConfiguration : IEntityTypeConfiguration<Deco>
{
    public void Configure(EntityTypeBuilder<Deco> builder)
    {
        builder.ToTable("Deco");

        builder.HasIndex(d => new { d.DecoGroupId, d.Name })
            .IsUnique();

        builder.HasMany(d => d.Playouts)
            .WithOne(p => p.Deco)
            .HasForeignKey(p => p.DecoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Watermark)
            .WithMany()
            .HasForeignKey(d => d.WatermarkId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
