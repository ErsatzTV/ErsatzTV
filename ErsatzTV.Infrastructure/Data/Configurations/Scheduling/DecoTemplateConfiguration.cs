using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoTemplateConfiguration : IEntityTypeConfiguration<DecoTemplate>
{
    public void Configure(EntityTypeBuilder<DecoTemplate> builder)
    {
        builder.ToTable("DecoTemplate");

        builder.HasIndex(d => new { d.DecoTemplateGroupId, d.Name })
            .IsUnique();

        builder.HasMany(b => b.Items)
            .WithOne(i => i.DecoTemplate)
            .HasForeignKey(i => i.DecoTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.PlayoutTemplates)
            .WithOne(t => t.DecoTemplate)
            .HasForeignKey(t => t.DecoTemplateId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
