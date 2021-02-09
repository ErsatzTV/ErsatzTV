using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexMediaSourceConnectionConfiguration : IEntityTypeConfiguration<PlexMediaSourceConnection>
    {
        public void Configure(EntityTypeBuilder<PlexMediaSourceConnection> builder) =>
            builder.ToTable("PlexMediaSourceConnections");
    }
}
