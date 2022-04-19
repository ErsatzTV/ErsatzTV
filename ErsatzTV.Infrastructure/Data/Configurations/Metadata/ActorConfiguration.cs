using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ActorConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> builder)
    {
        builder.ToTable("Actor");

        builder.HasOne(a => a.Artwork)
            .WithOne()
            .HasForeignKey<Actor>(a => a.ArtworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
