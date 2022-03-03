using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ArtworkConfiguration : IEntityTypeConfiguration<Artwork>
{
    public void Configure(EntityTypeBuilder<Artwork> builder) => builder.ToTable("Artwork");
}