using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexLibraryConfiguration : IEntityTypeConfiguration<PlexLibrary>
{
    public void Configure(EntityTypeBuilder<PlexLibrary> builder) =>
        builder.ToTable("PlexLibrary");
}
