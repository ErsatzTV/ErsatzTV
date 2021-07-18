using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MultiCollectionConfiguration : IEntityTypeConfiguration<MultiCollection>
    {
        public void Configure(EntityTypeBuilder<MultiCollection> builder)
        {
            builder.ToTable("MultiCollection");
            
            builder.HasMany(m => m.Collections)
                .WithMany(m => m.MultiCollections)
                .UsingEntity<MultiCollectionItem>(
                    j => j.HasOne(mci => mci.Collection)
                        .WithMany(c => c.MultiCollectionItems)
                        .HasForeignKey(mci => mci.CollectionId)
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(mci => mci.MultiCollection)
                        .WithMany(mc => mc.MultiCollectionItems)
                        .HasForeignKey(mci => mci.MultiCollectionId)
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey(mci => new { mci.MultiCollectionId, mci.CollectionId }));
        }
    }
}
