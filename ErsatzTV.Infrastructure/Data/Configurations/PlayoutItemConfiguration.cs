using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlayoutItemConfiguration : IEntityTypeConfiguration<PlayoutItem>

    {
        public void Configure(EntityTypeBuilder<PlayoutItem> builder) => builder.ToTable("PlayoutItem");
    }
}
