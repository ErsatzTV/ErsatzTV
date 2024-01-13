using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("Block");

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Block)
            .HasForeignKey(i => i.BlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.TemplateItems)
            .WithOne(i => i.Block)
            .HasForeignKey(i => i.BlockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
