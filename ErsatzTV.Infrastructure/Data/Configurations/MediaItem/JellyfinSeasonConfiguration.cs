using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class JellyfinSeasonConfiguration : IEntityTypeConfiguration<JellyfinSeason>
    {
        public void Configure(EntityTypeBuilder<JellyfinSeason> builder) => builder.ToTable("JellyfinSeason");
    }
}
