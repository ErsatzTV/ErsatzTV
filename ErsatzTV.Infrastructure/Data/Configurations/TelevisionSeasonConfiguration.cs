using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class TelevisionSeasonConfiguration : IEntityTypeConfiguration<TelevisionSeason>
    {
        public void Configure(EntityTypeBuilder<TelevisionSeason> builder)
        {
            builder.ToTable("TelevisionSeason");

            builder.HasMany(season => season.Episodes)
                .WithOne(episode => episode.Season)
                .HasForeignKey(episode => episode.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
