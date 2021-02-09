using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexMediaSourceLibraryConfiguration : IEntityTypeConfiguration<PlexMediaSourceLibrary>
    {
        public void Configure(EntityTypeBuilder<PlexMediaSourceLibrary> builder) =>
            builder.ToTable("PlexMediaSourceLibraries");
    }
}
