using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EmbyPathReplacementConfiguration : IEntityTypeConfiguration<EmbyPathReplacement>
{
    public void Configure(EntityTypeBuilder<EmbyPathReplacement> builder) =>
        builder.ToTable("EmbyPathReplacement");
}
