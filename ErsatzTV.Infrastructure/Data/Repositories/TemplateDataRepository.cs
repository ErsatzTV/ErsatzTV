using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class TemplateDataRepository(IDbContextFactory<TvContext> dbContextFactory) : ITemplateDataRepository
{
    public async Task<Option<Dictionary<string, object>>> GetMusicVideoTemplateData(
        Resolution resolution,
        TimeSpan streamSeek,
        int musicVideoId)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        Option<MusicVideo> maybeMusicVideo = await dbContext.MusicVideos
            .AsNoTracking()
            .Include(mv => mv.MediaVersions)
            .Include(mv => mv.Artist)
            .ThenInclude(a => a.ArtistMetadata)
            .Include(mv => mv.MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Artists)
            .Include(mv => mv.MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Studios)
            .Include(mv => mv.MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Directors)
            .SelectOneAsync(mv => mv.Id, mv => mv.Id == musicVideoId);

        foreach (var musicVideo in maybeMusicVideo)
        {
            foreach (var metadata in musicVideo.MusicVideoMetadata.HeadOrNone())
            {
                string artist = string.Empty;
                foreach (ArtistMetadata artistMetadata in Optional(musicVideo.Artist?.ArtistMetadata).Flatten())
                {
                    artist = artistMetadata.Title;
                }

                return new Dictionary<string, object>
                {
                    ["Resolution"] = resolution,
                    ["Title"] = metadata.Title,
                    ["Track"] = metadata.Track,
                    ["Album"] = metadata.Album,
                    ["Plot"] = metadata.Plot,
                    ["ReleaseDate"] = metadata.ReleaseDate,
                    ["Artists"] = (metadata.Artists ?? []).Map(a => a.Name).ToList(),
                    ["Artist"] = artist,
                    ["Studios"] = (metadata.Studios ?? []).Map(s => s.Name).ToList(),
                    ["Directors"] = (metadata.Directors ?? []).Map(s => s.Name).ToList(),
                    ["Duration"] = musicVideo.GetHeadVersion().Duration,
                    ["StreamSeek"] = streamSeek
                };
            }
        }

        return Option<Dictionary<string, object>>.None;
    }
}