using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ImageFolderDurationConfiguration : IEntityTypeConfiguration<ImageFolderDuration>
{
    public void Configure(EntityTypeBuilder<ImageFolderDuration> builder) => builder.ToTable("ImageFolderDuration");
}
