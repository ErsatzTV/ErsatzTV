using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MetadataGuidConfiguration : IEntityTypeConfiguration<MetadataGuid>
{
    public void Configure(EntityTypeBuilder<MetadataGuid> builder) => builder.ToTable("MetadataGuid");
}