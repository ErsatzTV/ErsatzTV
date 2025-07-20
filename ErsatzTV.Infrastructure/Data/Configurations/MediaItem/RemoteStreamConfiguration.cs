using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class RemoteStreamConfiguration : IEntityTypeConfiguration<RemoteStream>
{
    public void Configure(EntityTypeBuilder<RemoteStream> builder)
    {
        builder.ToTable("RemoteStream");

        builder.HasMany(i => i.RemoteStreamMetadata)
            .WithOne(m => m.RemoteStream)
            .HasForeignKey(m => m.RemoteStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.MediaVersions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
