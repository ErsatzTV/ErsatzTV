using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MediaStreamConfiguration : IEntityTypeConfiguration<MediaStream>
{
    public void Configure(EntityTypeBuilder<MediaStream> builder) => builder.ToTable("MediaStream");
}
