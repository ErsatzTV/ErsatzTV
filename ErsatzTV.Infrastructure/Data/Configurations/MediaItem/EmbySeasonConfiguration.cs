using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EmbySeasonConfiguration : IEntityTypeConfiguration<EmbySeason>
    {
        public void Configure(EntityTypeBuilder<EmbySeason> builder) => builder.ToTable("EmbySeason");
    }
}
