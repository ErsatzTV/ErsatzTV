using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder) =>
        builder.ToTable("Library");
}