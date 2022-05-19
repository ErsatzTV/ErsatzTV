using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EmbyCollectionConfiguration : IEntityTypeConfiguration<EmbyCollection>
{
    public void Configure(EntityTypeBuilder<EmbyCollection> builder) => builder.ToTable("EmbyCollection");
}
