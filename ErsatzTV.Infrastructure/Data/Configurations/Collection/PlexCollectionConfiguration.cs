using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexCollectionConfiguration : IEntityTypeConfiguration<PlexCollection>
{
    public void Configure(EntityTypeBuilder<PlexCollection> builder) => builder.ToTable("PlexCollection");
}
