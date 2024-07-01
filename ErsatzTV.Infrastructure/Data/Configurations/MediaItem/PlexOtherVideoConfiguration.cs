using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexOtherVideoConfiguration : IEntityTypeConfiguration<PlexOtherVideo>
{
    public void Configure(EntityTypeBuilder<PlexOtherVideo> builder) => builder.ToTable("PlexOtherVideo");
}
