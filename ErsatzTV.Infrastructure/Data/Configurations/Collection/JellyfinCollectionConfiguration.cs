using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinCollectionConfiguration : IEntityTypeConfiguration<JellyfinCollection>
{
    public void Configure(EntityTypeBuilder<JellyfinCollection> builder) => builder.ToTable("JellyfinCollection");
}
