using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class BlockGroupConfiguration : IEntityTypeConfiguration<BlockGroup>
{
    public void Configure(EntityTypeBuilder<BlockGroup> builder)
    {
        builder.ToTable("BlockGroup");
        
        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasMany(b => b.Blocks)
            .WithOne(i => i.BlockGroup)
            .HasForeignKey(i => i.BlockGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
