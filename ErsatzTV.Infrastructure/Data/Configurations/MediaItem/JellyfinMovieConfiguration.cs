using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class JellyfinMovieConfiguration : IEntityTypeConfiguration<JellyfinMovie>
    {
        public void Configure(EntityTypeBuilder<JellyfinMovie> builder) => builder.ToTable("JellyfinMovie");
    }
}
