using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoGroupConfiguration : IEntityTypeConfiguration<DecoGroup>
{
    public void Configure(EntityTypeBuilder<DecoGroup> builder)
    {
        builder.ToTable("DecoGroup");

        builder.Property(dg => dg.Name)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(dg => dg.Name)
            .IsUnique();

        builder.HasMany(dg => dg.Decos)
            .WithOne(d => d.DecoGroup)
            .HasForeignKey(d => d.DecoGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
