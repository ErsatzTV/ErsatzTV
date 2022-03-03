using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MultiCollectionItemConfiguration : IEntityTypeConfiguration<MultiCollectionItem>
{
    public void Configure(EntityTypeBuilder<MultiCollectionItem> builder) => builder.ToTable("MultiCollectionItem");
}