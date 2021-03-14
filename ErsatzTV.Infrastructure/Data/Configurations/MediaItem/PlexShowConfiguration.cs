using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexShowConfiguration : IEntityTypeConfiguration<PlexShow>
    {
        public void Configure(EntityTypeBuilder<PlexShow> builder) => builder.ToTable("PlexShow");
    }
}
