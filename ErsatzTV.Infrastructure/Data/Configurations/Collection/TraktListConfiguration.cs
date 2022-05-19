using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class TraktListConfiguration : IEntityTypeConfiguration<TraktList>
{
    public void Configure(EntityTypeBuilder<TraktList> builder)
    {
        builder.ToTable("TraktList");

        builder.HasMany(l => l.Items)
            .WithOne(i => i.TraktList)
            .HasForeignKey(i => i.TraktListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
