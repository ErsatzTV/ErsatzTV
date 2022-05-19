using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class SmartCollectionConfiguration : IEntityTypeConfiguration<SmartCollection>
{
    public void Configure(EntityTypeBuilder<SmartCollection> builder) => builder.ToTable("SmartCollection");
}
