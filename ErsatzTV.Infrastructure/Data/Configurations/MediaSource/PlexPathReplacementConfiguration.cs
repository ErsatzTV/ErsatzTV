using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexPathReplacementConfiguration : IEntityTypeConfiguration<PlexPathReplacement>
{
    public void Configure(EntityTypeBuilder<PlexPathReplacement> builder) => builder.ToTable("PlexPathReplacement");
}
