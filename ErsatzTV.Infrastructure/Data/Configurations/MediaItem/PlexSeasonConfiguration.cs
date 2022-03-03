using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexSeasonConfiguration : IEntityTypeConfiguration<PlexSeason>
{
    public void Configure(EntityTypeBuilder<PlexSeason> builder) => builder.ToTable("PlexSeason");
}