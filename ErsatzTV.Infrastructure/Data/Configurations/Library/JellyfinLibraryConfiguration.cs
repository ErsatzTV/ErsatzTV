using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class JellyfinLibraryConfiguration : IEntityTypeConfiguration<JellyfinLibrary>
    {
        public void Configure(EntityTypeBuilder<JellyfinLibrary> builder) =>
            builder.ToTable("JellyfinLibrary");
    }
}
