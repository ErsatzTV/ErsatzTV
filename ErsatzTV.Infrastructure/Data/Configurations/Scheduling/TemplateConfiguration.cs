using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Template");

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Template)
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.PlayoutTemplates)
            .WithOne(t => t.Template)
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
