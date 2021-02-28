using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class LibraryPathConfiguration : IEntityTypeConfiguration<LibraryPath>
    {
        public void Configure(EntityTypeBuilder<LibraryPath> builder)
        {
            builder.ToTable("LibraryPath");

            builder.HasMany(p => p.MediaItems)
                .WithOne(i => i.LibraryPath)
                .HasForeignKey(i => i.LibraryPathId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
