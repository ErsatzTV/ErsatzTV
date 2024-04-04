using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Scheduling;

public class DecoTemplateItemConfiguration : IEntityTypeConfiguration<DecoTemplateItem>
{
    public void Configure(EntityTypeBuilder<DecoTemplateItem> builder) => builder.ToTable("DecoTemplateItem");
}
