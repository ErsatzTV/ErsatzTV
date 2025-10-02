using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlayoutBuildStatusConfiguration : IEntityTypeConfiguration<PlayoutBuildStatus>
{
    public void Configure(EntityTypeBuilder<PlayoutBuildStatus> builder)
    {
        builder.ToTable("PlayoutBuildStatus");

        builder.HasKey(p => p.PlayoutId);
    }
}
