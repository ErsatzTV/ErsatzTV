using System.Globalization;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Infrastructure.Epg;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class TemplateDataRepository(ILocalFileSystem localFileSystem, IDbContextFactory<TvContext> dbContextFactory)
    : ITemplateDataRepository
{
    public async Task<Option<Dictionary<string, object>>> GetMediaItemTemplateData(MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie => await GetMovieTemplateData(mediaItem.Id),
            Episode => await GetEpisodeTemplateData(mediaItem.Id),
            MusicVideo => await GetMusicVideoTemplateData(mediaItem.Id),
            _ => Option<Dictionary<string, object>>.None
        };

    public async Task<Option<Dictionary<string, object>>> GetEpgTemplateData(
        string channelNumber,
        DateTimeOffset time,
        int count)
    {
        try
        {
            string targetFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{channelNumber}.xml");
            if (localFileSystem.FileExists(targetFile))
            {
                await using var stream = File.OpenRead(targetFile);
                var xmlProgrammes = EpgReader.FindProgrammesAt(stream, time, count);
                var result = new List<EpgProgrammeTemplateData>();

                foreach (var epgProgramme in xmlProgrammes)
                {
                    var data = new EpgProgrammeTemplateData
                    {
                        Title = epgProgramme.Title?.Value,
                        SubTitle = epgProgramme.SubTitle?.Value,
                        Description = epgProgramme.Description?.Value,
                        Rating = epgProgramme.Rating?.Value,
                        Categories = (epgProgramme.Categories ?? []).Map(c => c.Value).ToArray(),
                        Date = epgProgramme.Date?.Value
                    };

                    if (DateTimeOffset.TryParseExact(
                            epgProgramme.Start,
                            EpgReader.XmlTvDateFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var start))
                    {
                        data.Start = start.LocalDateTime;
                    }

                    if (DateTimeOffset.TryParseExact(
                            epgProgramme.Stop,
                            EpgReader.XmlTvDateFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var stop))
                    {
                        data.Stop = stop.LocalDateTime;
                    }

                    result.Add(data);
                }

                return new Dictionary<string, object>
                {
                    [EpgTemplateDataKey.Epg] = result
                };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return Option<Dictionary<string, object>>.None;
    }

    private async Task<Option<Dictionary<string, object>>> GetMovieTemplateData(int movieId)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        Option<Movie> maybeMovie = await dbContext.Movies
            .AsNoTracking()
            .Include(m => m.MediaVersions)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Genres)
            .SelectOneAsync(m => m.Id, m => m.Id == movieId);

        foreach (var movie in maybeMovie)
        {
            foreach (var metadata in movie.MovieMetadata.HeadOrNone())
            {
                return new Dictionary<string, object>
                {
                    [MediaItemTemplateDataKey.Title] = metadata.Title,
                    [MediaItemTemplateDataKey.Plot] = metadata.Plot,
                    [MediaItemTemplateDataKey.ReleaseDate] = metadata.ReleaseDate,
                    [MediaItemTemplateDataKey.Studios] = (metadata.Studios ?? []).Map(s => s.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Directors] = (metadata.Directors ?? []).Map(d => d.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Genres] = (metadata.Genres ?? []).Map(g => g.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Duration] = movie.GetHeadVersion().Duration
                };
            }
        }

        return Option<Dictionary<string, object>>.None;
    }

    private async Task<Option<Dictionary<string, object>>> GetEpisodeTemplateData(int episodeId)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        Option<Episode> maybeEpisode = await dbContext.Episodes
            .AsNoTracking()
            .Include(e => e.MediaVersions)
            .Include(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Studios)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Directors)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Genres)
            .SelectOneAsync(e => e.Id, e => e.Id == episodeId);

        var result = new Dictionary<string, object>();

        foreach (var episode in maybeEpisode)
        {
            foreach (var showMetadata in Optional(episode.Season?.Show?.ShowMetadata.HeadOrNone()).Flatten())
            {
                result.Add(MediaItemTemplateDataKey.ShowTitle, showMetadata.Title);
                result.Add(MediaItemTemplateDataKey.ShowYear, showMetadata.Year);
                result.Add(MediaItemTemplateDataKey.ShowContentRating, showMetadata.ContentRating);
                result.Add(MediaItemTemplateDataKey.ShowGenres,
                    (showMetadata.Genres ?? []).Map(s => s.Name).OrderBy(identity));
            }

            foreach (var metadata in episode.EpisodeMetadata.HeadOrNone())
            {
                result.Add(MediaItemTemplateDataKey.Title, metadata.Title);
                result.Add(MediaItemTemplateDataKey.Plot, metadata.Plot);
                result.Add(MediaItemTemplateDataKey.ReleaseDate, metadata.ReleaseDate);
                result.Add(MediaItemTemplateDataKey.Studios,
                    (metadata.Studios ?? []).Map(s => s.Name).OrderBy(identity));
                result.Add(MediaItemTemplateDataKey.Directors,
                    (metadata.Directors ?? []).Map(s => s.Name).OrderBy(identity));
                result.Add(MediaItemTemplateDataKey.Genres,
                    (metadata.Genres ?? []).Map(s => s.Name).OrderBy(identity));
                result.Add(MediaItemTemplateDataKey.Duration, episode.GetHeadVersion().Duration);
            }

            return result;
        }

        return Option<Dictionary<string, object>>.None;
    }

    private async Task<Option<Dictionary<string, object>>> GetMusicVideoTemplateData(int musicVideoId)
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
            .Include(mv => mv.MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Genres)
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
                    [MediaItemTemplateDataKey.Title] = metadata.Title,
                    [MediaItemTemplateDataKey.Track] = metadata.Track,
                    [MediaItemTemplateDataKey.Album] = metadata.Album,
                    [MediaItemTemplateDataKey.Plot] = metadata.Plot,
                    [MediaItemTemplateDataKey.ReleaseDate] = metadata.ReleaseDate,
                    [MediaItemTemplateDataKey.Artists] = (metadata.Artists ?? []).Map(a => a.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Artist] = artist,
                    [MediaItemTemplateDataKey.Studios] = (metadata.Studios ?? []).Map(s => s.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Directors] = (metadata.Directors ?? []).Map(d => d.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Genres] = (metadata.Genres ?? []).Map(g => g.Name).OrderBy(identity),
                    [MediaItemTemplateDataKey.Duration] = musicVideo.GetHeadVersion().Duration
                };
            }
        }

        return Option<Dictionary<string, object>>.None;
    }
}