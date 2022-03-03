using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EmbyMovieConfiguration : IEntityTypeConfiguration<EmbyMovie>
{
    public void Configure(EntityTypeBuilder<EmbyMovie> builder) => builder.ToTable("EmbyMovie");
}