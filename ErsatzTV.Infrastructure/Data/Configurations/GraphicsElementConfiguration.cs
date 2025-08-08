using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class GraphicsElementConfiguration : IEntityTypeConfiguration<GraphicsElement>
{
    public void Configure(EntityTypeBuilder<GraphicsElement> builder) => builder.ToTable("GraphicsElement");
}