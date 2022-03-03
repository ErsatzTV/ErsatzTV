using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder) => builder.ToTable("Studio");
}