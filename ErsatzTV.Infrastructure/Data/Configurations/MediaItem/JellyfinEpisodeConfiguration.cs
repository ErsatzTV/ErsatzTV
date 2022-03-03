using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class JellyfinEpisodeConfiguration : IEntityTypeConfiguration<JellyfinEpisode>
{
    public void Configure(EntityTypeBuilder<JellyfinEpisode> builder) => builder.ToTable("JellyfinEpisode");
}