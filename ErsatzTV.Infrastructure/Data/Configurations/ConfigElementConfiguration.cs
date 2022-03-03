using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ConfigElementConfiguration : IEntityTypeConfiguration<ConfigElement>
{
    public void Configure(EntityTypeBuilder<ConfigElement> builder)
    {
        builder.ToTable("ConfigElement");

        builder.HasIndex(ce => ce.Key)
            .IsUnique();
    }
}