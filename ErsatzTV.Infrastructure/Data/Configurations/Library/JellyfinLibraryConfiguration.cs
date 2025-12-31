using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinLibraryConfiguration : IEntityTypeConfiguration<JellyfinLibrary>
{
    public void Configure(EntityTypeBuilder<JellyfinLibrary> builder)
    {
        builder.ToTable("JellyfinLibrary");

        builder.HasMany(l => l.PathInfos)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.ItemId)
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.HasIndex(l => l.ItemId);
    }
}
