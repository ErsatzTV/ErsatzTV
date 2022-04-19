using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexMediaFileConfiguration : IEntityTypeConfiguration<PlexMediaFile>
{
    public void Configure(EntityTypeBuilder<PlexMediaFile> builder) => builder.ToTable("PlexMediaFile");
}
