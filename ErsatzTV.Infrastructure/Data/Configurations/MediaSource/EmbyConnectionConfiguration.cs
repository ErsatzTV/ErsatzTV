using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class EmbyConnectionConfiguration : IEntityTypeConfiguration<EmbyConnection>
    {
        public void Configure(EntityTypeBuilder<EmbyConnection> builder) =>
            builder.ToTable("EmbyConnection");
    }
}
