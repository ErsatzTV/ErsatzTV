using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class TemplateGroupConfiguration : IEntityTypeConfiguration<TemplateGroup>
{
    public void Configure(EntityTypeBuilder<TemplateGroup> builder)
    {
        builder.ToTable("TemplateGroup");

        builder.Property(t => t.Name)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasMany(b => b.Templates)
            .WithOne(i => i.TemplateGroup)
            .HasForeignKey(i => i.TemplateGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
