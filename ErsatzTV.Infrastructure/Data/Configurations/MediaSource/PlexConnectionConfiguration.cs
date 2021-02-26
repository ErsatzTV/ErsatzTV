using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexConnectionConfiguration : IEntityTypeConfiguration<PlexConnection>
    {
        public void Configure(EntityTypeBuilder<PlexConnection> builder) =>
            builder.ToTable("PlexConnection");
    }
}
