using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoTemplateGroupConfiguration : IEntityTypeConfiguration<DecoTemplateGroup>
{
    public void Configure(EntityTypeBuilder<DecoTemplateGroup> builder)
    {
        builder.ToTable("DecoTemplateGroup");

        builder.Property(d => d.Name)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasMany(b => b.DecoTemplates)
            .WithOne(i => i.DecoTemplateGroup)
            .HasForeignKey(i => i.DecoTemplateGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
