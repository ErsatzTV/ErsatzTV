using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class DirectorConfiguration : IEntityTypeConfiguration<Director>
{
    public void Configure(EntityTypeBuilder<Director> builder) => builder.ToTable("Director");
}