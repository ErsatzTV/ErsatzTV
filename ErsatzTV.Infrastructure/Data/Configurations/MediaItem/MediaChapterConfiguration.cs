using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MediaChapterConfiguration : IEntityTypeConfiguration<MediaChapter>
{
    public void Configure(EntityTypeBuilder<MediaChapter> builder) => builder.ToTable("MediaChapter");
}