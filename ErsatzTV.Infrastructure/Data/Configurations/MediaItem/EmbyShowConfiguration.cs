using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class EmbyShowConfiguration : IEntityTypeConfiguration<EmbyShow>
{
    public void Configure(EntityTypeBuilder<EmbyShow> builder) => builder.ToTable("EmbyShow");
}