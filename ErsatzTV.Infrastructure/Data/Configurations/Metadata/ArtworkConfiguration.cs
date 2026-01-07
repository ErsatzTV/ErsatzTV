using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class ArtworkConfiguration : IEntityTypeConfiguration<Artwork>
{
    public void Configure(EntityTypeBuilder<Artwork> builder)
    {
        builder.ToTable("Artwork");

        string[] fkColumns =
        [
            "ArtistMetadataId",
            "ChannelId",
            "EpisodeMetadataId",
            "MovieMetadataId",
            "MusicVideoMetadataId",
            "OtherVideoMetadataId",
            "SeasonMetadataId",
            "ShowMetadataId",
            "SongMetadataId",
            "ImageMetadataId",
            "RemoteStreamMetadataId"
        ];

        var coalesceList = string.Join(", ", fkColumns);
        var computedSql = $"CASE WHEN COALESCE({coalesceList}) IS NULL THEN 1 ELSE NULL END";

        builder.Property(a => a.IsMetadataOrphan)
            .HasComputedColumnSql(computedSql, stored: false);

        builder.HasIndex(a => a.IsMetadataOrphan);
    }
}
