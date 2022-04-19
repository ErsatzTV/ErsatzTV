using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EmbyLibraryConfiguration : IEntityTypeConfiguration<EmbyLibrary>
{
    public void Configure(EntityTypeBuilder<EmbyLibrary> builder) =>
        builder.ToTable("EmbyLibrary");
}
