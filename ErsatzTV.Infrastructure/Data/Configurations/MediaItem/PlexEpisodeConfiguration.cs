using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlexEpisodeConfiguration : IEntityTypeConfiguration<PlexEpisode>
{
    public void Configure(EntityTypeBuilder<PlexEpisode> builder) => builder.ToTable("PlexEpisode");
}