using System.Globalization;
using System.Xml;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Newtonsoft.Json;

namespace ErsatzTV.Application.Channels;

public class RefreshChannelDataHandler : IRequestHandler<RefreshChannelData>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<RefreshChannelDataHandler> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RefreshChannelDataHandler(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem,
        ILogger<RefreshChannelDataHandler> logger)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task Handle(RefreshChannelData request, CancellationToken cancellationToken)
    {
        _localFileSystem.EnsureFolderExists(FileSystemLayout.ChannelGuideCacheFolder);

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Playout> playouts = await dbContext.Playouts
            .AsNoTracking()
            .Filter(pi => pi.Channel.Number == request.ChannelNumber)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).EpisodeMetadata)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(em => em.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).Artist)
            .ThenInclude(a => a.ArtistMetadata)
            .ThenInclude(am => am.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).OtherVideoMetadata)
            .ThenInclude(vm => vm.Artwork)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Song).SongMetadata)
            .ThenInclude(vm => vm.Artwork)
            .ToListAsync(cancellationToken);

        List<PlayoutItem> sorted = [];
        
        foreach (Playout playout in playouts)
        {
            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Flood:
                case ProgramSchedulePlayoutType.Block:
                    sorted.AddRange(playouts.Collect(p => p.Items).OrderBy(pi => pi.Start));
                    break;
                case ProgramSchedulePlayoutType.ExternalJson:
                    sorted.AddRange(await CollectExternalJsonItems(playout.ExternalJsonFile));
                    break;
            }
        }

        await using RecyclableMemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        await using var xml = XmlWriter.Create(
            ms,
            new XmlWriterSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment });

        // skip all filler that isn't pre-roll
        var i = 0;
        while (i < sorted.Count && sorted[i].FillerKind != FillerKind.None &&
               sorted[i].FillerKind != FillerKind.PreRoll)
        {
            i++;
        }

        while (i < sorted.Count)
        {
            PlayoutItem startItem = sorted[i];
            int j = i;
            while (sorted[j].FillerKind != FillerKind.None && j + 1 < sorted.Count)
            {
                j++;
            }

            PlayoutItem displayItem = sorted[j];
            bool hasCustomTitle = !string.IsNullOrWhiteSpace(startItem.CustomTitle);

            int finishIndex = j;
            while (finishIndex + 1 < sorted.Count && (sorted[finishIndex + 1].GuideGroup == startItem.GuideGroup
                                                      || sorted[finishIndex + 1].FillerKind is FillerKind.GuideMode
                                                          or FillerKind.Tail or FillerKind.Fallback))
            {
                finishIndex++;
            }

            int customShowId = -1;
            if (displayItem.MediaItem is Episode ep)
            {
                customShowId = ep.Season.ShowId;
            }

            bool isSameCustomShow = hasCustomTitle;
            for (int x = j; x <= finishIndex; x++)
            {
                isSameCustomShow = isSameCustomShow && sorted[x].MediaItem is Episode e &&
                                   customShowId == e.Season.ShowId;
            }

            PlayoutItem finishItem = sorted[finishIndex];
            i = finishIndex;

            string start = startItem.StartOffset.ToString("yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture)
                .Replace(":", string.Empty);
            string stop = displayItem.GuideFinishOffset.HasValue
                ? displayItem.GuideFinishOffset.Value.ToString("yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture)
                    .Replace(":", string.Empty)
                : finishItem.FinishOffset.ToString("yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture)
                    .Replace(":", string.Empty);

            string title = GetTitle(displayItem);
            string subtitle = GetSubtitle(displayItem);
            string description = GetDescription(displayItem);
            Option<ContentRating> contentRating = GetContentRating(displayItem);

            await xml.WriteStartElementAsync(null, "programme", null);
            await xml.WriteAttributeStringAsync(null, "start", null, start);
            await xml.WriteAttributeStringAsync(null, "stop", null, stop);
            await xml.WriteAttributeStringAsync(null, "channel", null, $"{request.ChannelNumber}.etv");

            await xml.WriteStartElementAsync(null, "title", null);
            await xml.WriteAttributeStringAsync(null, "lang", null, "en");
            await xml.WriteStringAsync(title);
            await xml.WriteEndElementAsync(); // title

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                await xml.WriteStartElementAsync(null, "sub-title", null);
                await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                await xml.WriteStringAsync(subtitle);
                await xml.WriteEndElementAsync(); // subtitle
            }

            if (!isSameCustomShow)
            {
                if (!string.IsNullOrWhiteSpace(description))
                {
                    await xml.WriteStartElementAsync(null, "desc", null);
                    await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                    await xml.WriteStringAsync(description);
                    await xml.WriteEndElementAsync(); // desc
                }
            }

            if (!hasCustomTitle && displayItem.MediaItem is Movie movie)
            {
                foreach (MovieMetadata metadata in movie.MovieMetadata.HeadOrNone())
                {
                    if (metadata.Year.HasValue)
                    {
                        await xml.WriteStartElementAsync(null, "date", null);
                        await xml.WriteStringAsync(metadata.Year.Value.ToString(CultureInfo.InvariantCulture));
                        await xml.WriteEndElementAsync(); // date
                    }

                    await xml.WriteStartElementAsync(null, "category", null);
                    await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                    await xml.WriteStringAsync("Movie");
                    await xml.WriteEndElementAsync(); // category

                    foreach (Genre genre in Optional(metadata.Genres).Flatten().OrderBy(g => g.Name))
                    {
                        await xml.WriteStartElementAsync(null, "category", null);
                        await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                        await xml.WriteStringAsync(genre.Name);
                        await xml.WriteEndElementAsync(); // category
                    }

                    string poster = Optional(metadata.Artwork).Flatten()
                        .Filter(a => a.ArtworkKind == ArtworkKind.Poster)
                        .HeadOrNone()
                        .Match(a => GetArtworkUrl(a, ArtworkKind.Poster), () => string.Empty);

                    if (!string.IsNullOrWhiteSpace(poster))
                    {
                        await xml.WriteStartElementAsync(null, "icon", null);
                        await xml.WriteAttributeStringAsync(null, "src", null, poster);
                        await xml.WriteEndElementAsync(); // icon
                    }
                }
            }

            if (!hasCustomTitle && displayItem.MediaItem is MusicVideo musicVideo)
            {
                foreach (MusicVideoMetadata metadata in musicVideo.MusicVideoMetadata.HeadOrNone())
                {
                    if (metadata.Year.HasValue)
                    {
                        await xml.WriteStartElementAsync(null, "date", null);
                        await xml.WriteStringAsync(metadata.Year.Value.ToString(CultureInfo.InvariantCulture));
                        await xml.WriteEndElementAsync(); // date
                    }

                    await xml.WriteStartElementAsync(null, "category", null);
                    await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                    await xml.WriteStringAsync("Music");
                    await xml.WriteEndElementAsync(); // category

                    // music video genres
                    foreach (Genre genre in Optional(metadata.Genres).Flatten().OrderBy(g => g.Name))
                    {
                        await xml.WriteStartElementAsync(null, "category", null);
                        await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                        await xml.WriteStringAsync(genre.Name);
                        await xml.WriteEndElementAsync(); // category
                    }

                    // artist genres
                    Option<ArtistMetadata> maybeMetadata =
                        Optional(musicVideo.Artist?.ArtistMetadata.HeadOrNone()).Flatten();
                    foreach (ArtistMetadata artistMetadata in maybeMetadata)
                    {
                        foreach (Genre genre in Optional(artistMetadata.Genres).Flatten().OrderBy(g => g.Name))
                        {
                            await xml.WriteStartElementAsync(null, "category", null);
                            await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                            await xml.WriteStringAsync(genre.Name);
                            await xml.WriteEndElementAsync(); // category
                        }
                    }

                    string artworkPath = GetPrioritizedArtworkPath(metadata);
                    if (!string.IsNullOrWhiteSpace(artworkPath))
                    {
                        await xml.WriteStartElementAsync(null, "icon", null);
                        await xml.WriteAttributeStringAsync(null, "src", null, artworkPath);
                        await xml.WriteEndElementAsync(); // icon
                    }
                }
            }

            if (!hasCustomTitle && displayItem.MediaItem is Song song)
            {
                await xml.WriteStartElementAsync(null, "category", null);
                await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                await xml.WriteStringAsync("Music");
                await xml.WriteEndElementAsync(); // category

                foreach (SongMetadata metadata in song.SongMetadata.HeadOrNone())
                {
                    string artworkPath = GetPrioritizedArtworkPath(metadata);
                    if (!string.IsNullOrWhiteSpace(artworkPath))
                    {
                        await xml.WriteStartElementAsync(null, "icon", null);
                        await xml.WriteAttributeStringAsync(null, "src", null, artworkPath);
                        await xml.WriteEndElementAsync(); // icon
                    }
                }
            }

            if (displayItem.MediaItem is Episode episode && (!hasCustomTitle || isSameCustomShow))
            {
                Option<ShowMetadata> maybeMetadata =
                    Optional(episode.Season?.Show?.ShowMetadata.HeadOrNone()).Flatten();
                foreach (ShowMetadata metadata in maybeMetadata)
                {
                    await xml.WriteStartElementAsync(null, "category", null);
                    await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                    await xml.WriteStringAsync("Series");
                    await xml.WriteEndElementAsync(); // category

                    foreach (Genre genre in Optional(metadata.Genres).Flatten().OrderBy(g => g.Name))
                    {
                        await xml.WriteStartElementAsync(null, "category", null);
                        await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                        await xml.WriteStringAsync(genre.Name);
                        await xml.WriteEndElementAsync(); // category
                    }

                    string artworkPath = GetPrioritizedArtworkPath(metadata);
                    if (!string.IsNullOrWhiteSpace(artworkPath))
                    {
                        await xml.WriteStartElementAsync(null, "icon", null);
                        await xml.WriteAttributeStringAsync(null, "src", null, artworkPath);
                        await xml.WriteEndElementAsync(); // icon
                    }
                }

                if (!isSameCustomShow)
                {
                    int s = await Optional(episode.Season?.SeasonNumber).IfNoneAsync(-1);
                    // TODO: multi-episode?
                    int e = episode.EpisodeMetadata.HeadOrNone().Match(em => em.EpisodeNumber, -1);
                    if (s >= 0 && e > 0)
                    {
                        await xml.WriteStartElementAsync(null, "episode-num", null);
                        await xml.WriteAttributeStringAsync(null, "system", null, "onscreen");
                        await xml.WriteStringAsync($"S{s:00}E{e:00}");
                        await xml.WriteEndElementAsync(); // episode-num

                        await xml.WriteStartElementAsync(null, "episode-num", null);
                        await xml.WriteAttributeStringAsync(null, "system", null, "xmltv_ns");
                        await xml.WriteStringAsync($"{s - 1}.{e - 1}.0/1");
                        await xml.WriteEndElementAsync(); // episode-num
                    }
                }
            }

            await xml.WriteStartElementAsync(null, "previously-shown", null);
            await xml.WriteEndElementAsync(); // previously-shown

            foreach (ContentRating rating in contentRating)
            {
                await xml.WriteStartElementAsync(null, "rating", null);
                foreach (string system in rating.System)
                {
                    await xml.WriteAttributeStringAsync(null, "system", null, system);
                }

                await xml.WriteStartElementAsync(null, "value", null);
                await xml.WriteStringAsync(rating.Value);
                await xml.WriteEndElementAsync(); // value
                await xml.WriteEndElementAsync(); // rating
            }

            await xml.WriteEndElementAsync(); // programme

            i++;
        }

        await xml.FlushAsync();

        string tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, ms.ToArray(), cancellationToken);

        string targetFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{request.ChannelNumber}.xml");
        File.Move(tempFile, targetFile, true);
    }

    private static string GetArtworkUrl(Artwork artwork, ArtworkKind artworkKind)
    {
        string artworkPath = artwork.Path;

        int height = artworkKind switch
        {
            ArtworkKind.Thumbnail => 220,
            _ => 440
        };

        if (artworkPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || artworkPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return artworkPath;
        }
        if (artworkPath.StartsWith("jellyfin://", StringComparison.OrdinalIgnoreCase))
        {
            artworkPath = JellyfinUrl.PlaceholderProxyForArtwork(artworkPath, artworkKind, height);
        }
        else if (artworkPath.StartsWith("emby://", StringComparison.OrdinalIgnoreCase))
        {
            artworkPath = EmbyUrl.PlaceholderProxyForArtwork(artworkPath, artworkKind, height);
        }
        else
        {
            string artworkFolder = artworkKind switch
            {
                ArtworkKind.Thumbnail => "thumbnails",
                _ => "posters"
            };

            artworkPath = $"{{RequestBase}}/iptv/artwork/{artworkFolder}/{artwork.Path}.jpg{{AccessTokenUri}}";
        }

        return artworkPath;
    }

    private static string GetTitle(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return playoutItem.CustomTitle;
        }

        return playoutItem.MediaItem switch
        {
            Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title ?? string.Empty)
                .IfNone("[unknown movie]"),
            Episode e => e.Season.Show.ShowMetadata.HeadOrNone().Map(em => em.Title ?? string.Empty)
                .IfNone("[unknown show]"),
            MusicVideo mv => mv.Artist.ArtistMetadata.HeadOrNone().Map(am => am.Title ?? string.Empty)
                .IfNone("[unknown artist]"),
            OtherVideo ov => ov.OtherVideoMetadata.HeadOrNone().Map(vm => vm.Title ?? string.Empty)
                .IfNone("[unknown video]"),
            Song s => s.SongMetadata.HeadOrNone().Map(sm => sm.Artist ?? string.Empty)
                .IfNone("[unknown artist]"),
            _ => "[unknown]"
        };
    }

    private static string GetSubtitle(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return string.Empty;
        }

        return playoutItem.MediaItem switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.Title ?? string.Empty,
                () => string.Empty),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                mvm => mvm.Title ?? string.Empty,
                () => string.Empty),
            Song s => s.SongMetadata.HeadOrNone().Match(
                mvm => mvm.Title ?? string.Empty,
                () => string.Empty),
            _ => string.Empty
        };
    }

    private static string GetDescription(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return string.Empty;
        }

        return playoutItem.MediaItem switch
        {
            Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Plot ?? string.Empty).IfNone(string.Empty),
            Episode e => e.EpisodeMetadata.HeadOrNone().Map(em => em.Plot ?? string.Empty)
                .IfNone(string.Empty),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Map(mvm => mvm.Plot ?? string.Empty)
                .IfNone(string.Empty),
            OtherVideo ov => ov.OtherVideoMetadata.HeadOrNone().Map(ovm => ovm.Plot ?? string.Empty)
                .IfNone(string.Empty),
            _ => string.Empty
        };
    }

    private Option<ContentRating> GetContentRating(PlayoutItem playoutItem)
    {
        try
        {
            return playoutItem.MediaItem switch
            {
                Movie m => m.MovieMetadata
                    .HeadOrNone()
                    .Match(mm => ParseContentRating(mm.ContentRating, "MPAA"), () => None),
                Episode e => e.Season.Show.ShowMetadata
                    .HeadOrNone()
                    .Match(sm => ParseContentRating(sm.ContentRating, "VCHIP"), () => None),
                _ => None
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get content rating for playout item {Item}", GetTitle(playoutItem));
            return None;
        }
    }

    private static Option<ContentRating> ParseContentRating(string contentRating, string system)
    {
        Option<string> maybeFirst = (contentRating ?? string.Empty).Split('/').HeadOrNone();
        return maybeFirst.Map(
            first =>
            {
                string[] split = first.Split(':');
                if (split.Length == 2)
                {
                    return split[0].Equals("us", StringComparison.OrdinalIgnoreCase)
                        ? new ContentRating(system, split[1].ToUpperInvariant())
                        : new ContentRating(None, split[1].ToUpperInvariant());
                }

                return string.IsNullOrWhiteSpace(first)
                    ? Option<ContentRating>.None
                    : new ContentRating(None, first);
            }).Flatten();
    }

    private static string GetPrioritizedArtworkPath(Metadata metadata)
    {
        Option<string> maybeArtwork = Optional(metadata.Artwork).Flatten()
            .Filter(a => a.ArtworkKind == ArtworkKind.Poster)
            .HeadOrNone()
            .Map(a => GetArtworkUrl(a, ArtworkKind.Poster));

        if (maybeArtwork.IsNone)
        {
            maybeArtwork = Optional(metadata.Artwork).Flatten()
                .Filter(a => a.ArtworkKind == ArtworkKind.Thumbnail)
                .HeadOrNone()
                .Map(a => GetArtworkUrl(a, ArtworkKind.Thumbnail));
        }

        return maybeArtwork.IfNone(string.Empty);
    }

    private async Task<List<PlayoutItem>> CollectExternalJsonItems(string path)
    {
        var result = new List<PlayoutItem>();

        if (_localFileSystem.FileExists(path))
        {
            Option<ExternalJsonChannel> maybeChannel = JsonConvert.DeserializeObject<ExternalJsonChannel>(
                await File.ReadAllTextAsync(path));

            // must deserialize channel from json
            foreach (ExternalJsonChannel channel in maybeChannel)
            {
                // TODO: null start time should log and throw
            
                DateTimeOffset startTime = DateTimeOffset.Parse(
                    channel.StartTime ?? string.Empty,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal).ToLocalTime();

                for (var i = 0; i < channel.Programs.Length; i++)
                {
                    ExternalJsonProgram program = channel.Programs[i];
                    int milliseconds = program.Duration;
                    DateTimeOffset nextStart = startTime + TimeSpan.FromMilliseconds(milliseconds);
                    if (program.Duration >= channel.GuideMinimumDurationSeconds * 1000)
                    {
                        result.Add(BuildPlayoutItem(startTime, program, i));
                    }

                    startTime = nextStart;
                }
            }
        }

        return result;
    }

    private static PlayoutItem BuildPlayoutItem(DateTimeOffset startTime, ExternalJsonProgram program, int count)
    {
        MediaItem mediaItem = program.Type switch
        {
            "episode" => BuildEpisode(program),
            _ => BuildMovie(program)
        };

        return new PlayoutItem
        {
            Start = startTime.UtcDateTime,
            Finish = startTime.AddMilliseconds(program.Duration).UtcDateTime,
            FillerKind = FillerKind.None,
            ChapterTitle = null,
            GuideFinish = null,
            GuideGroup = count,
            CustomTitle = null,
            InPoint = TimeSpan.Zero,
            OutPoint = TimeSpan.FromMilliseconds(program.Duration),
            MediaItem = mediaItem
        };
    }

    private static Episode BuildEpisode(ExternalJsonProgram program)
    {
        var artwork = new List<Artwork>();
        if (!string.IsNullOrWhiteSpace(program.Icon))
        {
            artwork.Add(new Artwork
            {
                ArtworkKind = ArtworkKind.Thumbnail,
                Path = program.Icon,
                SourcePath = program.Icon
            });
        }
        
        return new Episode
        {
            MediaVersions =
            [
                new MediaVersion
                {
                    Duration = TimeSpan.FromMilliseconds(program.Duration)
                }
            ],
            EpisodeMetadata =
            [
                new EpisodeMetadata
                {
                    EpisodeNumber = program.Episode,
                    Title = program.Title
                },
            ],
            Season = new Season
            {
                SeasonNumber = program.Season,
                Show = new Show
                {
                    ShowMetadata =
                    [
                        new ShowMetadata
                        {
                            Title = program.ShowTitle,
                            Artwork = artwork
                        }
                    ]
                }
            }
        };
    }

    private static Movie BuildMovie(ExternalJsonProgram program)
    {
        var artwork = new List<Artwork>();
        if (!string.IsNullOrWhiteSpace(program.Icon))
        {
            artwork.Add(new Artwork
            {
                ArtworkKind = ArtworkKind.Poster,
                Path = program.Icon,
                SourcePath = program.Icon
            });
        }

        return new Movie
        {
            MediaVersions =
            [
                new MediaVersion
                {
                    Duration = TimeSpan.FromMilliseconds(program.Duration)
                }
            ],
            MovieMetadata =
            [
                new MovieMetadata
                {
                    Title = program.Title,
                    Year = program.Year,
                    Artwork = artwork
                }
            ]
        };
    }

    private sealed record ContentRating(Option<string> System, string Value);
}
