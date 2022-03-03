using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class LocalLibraryConfiguration : IEntityTypeConfiguration<LocalLibrary>
{
    public void Configure(EntityTypeBuilder<LocalLibrary> builder) =>
        builder.ToTable("LocalLibrary");
}