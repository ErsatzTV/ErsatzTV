using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class JellyfinConnectionConfiguration : IEntityTypeConfiguration<JellyfinConnection>
    {
        public void Configure(EntityTypeBuilder<JellyfinConnection> builder) =>
            builder.ToTable("JellyfinConnection");
    }
}
