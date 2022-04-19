using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class LocalMediaSourceConfiguration : IEntityTypeConfiguration<LocalMediaSource>
{
    public void Configure(EntityTypeBuilder<LocalMediaSource> builder) =>
        builder.ToTable("LocalMediaSource");
}
