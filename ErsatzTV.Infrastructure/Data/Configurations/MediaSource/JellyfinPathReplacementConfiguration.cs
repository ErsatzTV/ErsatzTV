using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinPathReplacementConfiguration : IEntityTypeConfiguration<JellyfinPathReplacement>
{
    public void Configure(EntityTypeBuilder<JellyfinPathReplacement> builder) =>
        builder.ToTable("JellyfinPathReplacement");
}