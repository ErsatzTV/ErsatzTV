using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexMovieMediaItemConfiguration : IEntityTypeConfiguration<PlexMovie>
    {
        public void Configure(EntityTypeBuilder<PlexMovie> builder) => builder.ToTable("PlexMovie");
    }
}
