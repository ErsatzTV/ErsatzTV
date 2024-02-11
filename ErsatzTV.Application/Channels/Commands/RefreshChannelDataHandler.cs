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
using Scriban;
using Scriban.Runtime;
using WebMarkupMin.Core;

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

        string movieTemplateFileName = GetMovieTemplateFileName();
        string episodeTemplateFileName = GetEpisodeTemplateFileName();
        string musicVideoTemplateFileName = GetMusicVideoTemplateFileName();
        string songTemplateFileName = GetSongTemplateFileName();
        string otherVideoTemplateFileName = GetOtherVideoTemplateFileName();
        if (movieTemplateFileName is null || episodeTemplateFileName is null || musicVideoTemplateFileName is null ||
            songTemplateFileName is null || otherVideoTemplateFileName is null)
        {
            return;
        }

        var minifier = new XmlMinifier(
            new XmlMinificationSettings
            {
                MinifyWhitespace = true,
                RemoveXmlComments = true,
                CollapseTagsWithoutContent = true
            });

        var templateContext = new XmlTemplateContext();
        
        string movieText = await File.ReadAllTextAsync(movieTemplateFileName, cancellationToken);
        var movieTemplate = Template.Parse(movieText, movieTemplateFileName);

        string episodeText = await File.ReadAllTextAsync(episodeTemplateFileName, cancellationToken);
        var episodeTemplate = Template.Parse(episodeText, episodeTemplateFileName);

        string musicVideoText = await File.ReadAllTextAsync(musicVideoTemplateFileName, cancellationToken);
        var musicVideoTemplate = Template.Parse(musicVideoText, musicVideoTemplateFileName);

        string songText = await File.ReadAllTextAsync(songTemplateFileName, cancellationToken);
        var songTemplate = Template.Parse(songText, songTemplateFileName);

        string otherVideoText = await File.ReadAllTextAsync(otherVideoTemplateFileName, cancellationToken);
        var otherVideoTemplate = Template.Parse(otherVideoText, otherVideoTemplateFileName);

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Playout> playouts = await dbContext.Playouts
            .AsNoTracking()
            .Filter(pi => pi.Channel.Number == request.ChannelNumber)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).EpisodeMetadata)
            .ThenInclude(em => em.Guids)
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
            .ThenInclude(sm => sm.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Guids)
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
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Guids)
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
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Studios)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Directors)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Artists)
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
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Song).SongMetadata)
            .ThenInclude(sm => sm.Genres)
            .Include(p => p.Items)
            .ThenInclude(i => i.MediaItem)
            .ThenInclude(i => (i as Song).SongMetadata)
            .ThenInclude(sm => sm.Studios)
            .ToListAsync(cancellationToken);

        await using RecyclableMemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        await using var xml = XmlWriter.Create(
            ms,
            new XmlWriterSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment });

        foreach (Playout playout in playouts)
        {
            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Flood:
                    var floodSorted = playouts.Collect(p => p.Items).OrderBy(pi => pi.Start).ToList();
                    await WritePlayoutXml(
                        request,
                        floodSorted,
                        templateContext,
                        movieTemplate,
                        episodeTemplate,
                        musicVideoTemplate,
                        songTemplate,
                        otherVideoTemplate,
                        minifier,
                        xml);
                    break;
                case ProgramSchedulePlayoutType.Block:
                    var blockSorted = playouts.Collect(p => p.Items).OrderBy(pi => pi.Start).ToList();
                    await WriteBlockPlayoutXml(
                        request,
                        blockSorted,
                        templateContext,
                        movieTemplate,
                        episodeTemplate,
                        musicVideoTemplate,
                        songTemplate,
                        otherVideoTemplate,
                        minifier,
                        xml);
                    break;
                case ProgramSchedulePlayoutType.ExternalJson:
                    List<PlayoutItem> externalJsonSorted = await CollectExternalJsonItems(playout.ExternalJsonFile);
                    await WritePlayoutXml(
                        request,
                        externalJsonSorted,
                        templateContext,
                        movieTemplate,
                        episodeTemplate,
                        musicVideoTemplate,
                        songTemplate,
                        otherVideoTemplate,
                        minifier,
                        xml);
                    break;
            }
        }

        await xml.FlushAsync();

        string tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, ms.ToArray(), cancellationToken);

        string targetFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{request.ChannelNumber}.xml");
        File.Move(tempFile, targetFile, true);
    }

    private async Task WritePlayoutXml(
        RefreshChannelData request,
        List<PlayoutItem> sorted,
        XmlTemplateContext templateContext,
        Template movieTemplate,
        Template episodeTemplate,
        Template musicVideoTemplate,
        Template songTemplate,
        Template otherVideoTemplate,
        XmlMinifier minifier,
        XmlWriter xml)
    {
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

            await WriteItemToXml(
                request,
                displayItem,
                start,
                stop,
                hasCustomTitle,
                templateContext,
                movieTemplate,
                episodeTemplate,
                musicVideoTemplate,
                songTemplate,
                otherVideoTemplate,
                minifier,
                xml);

            i++;
        }
    }
    
    private async Task WriteBlockPlayoutXml(
        RefreshChannelData request,
        List<PlayoutItem> sorted,
        XmlTemplateContext templateContext,
        Template movieTemplate,
        Template episodeTemplate,
        Template musicVideoTemplate,
        Template songTemplate,
        Template otherVideoTemplate,
        XmlMinifier minifier,
        XmlWriter xml)
    {
        var groups = sorted.GroupBy(s => new { s.GuideStart, s.GuideFinish, s.GuideGroup });
        foreach (var group in groups)
        {
            DateTime groupStart = group.Key.GuideStart!.Value;
            DateTime groupFinish = group.Key.GuideFinish!.Value;
            TimeSpan groupDuration = groupFinish - groupStart;

            var itemsToInclude = group.Filter(g => g.FillerKind is FillerKind.None).ToList();
            TimeSpan perItem = groupDuration / itemsToInclude.Count;

            DateTimeOffset currentStart = new DateTimeOffset(groupStart, TimeSpan.Zero).ToLocalTime();
            DateTimeOffset currentFinish = currentStart + perItem;

            foreach (PlayoutItem item in itemsToInclude)
            {
                string start = currentStart.ToString("yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture)
                    .Replace(":", string.Empty);
                string stop = currentFinish.ToString("yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture)
                    .Replace(":", string.Empty);

                await WriteItemToXml(
                    request,
                    item,
                    start,
                    stop,
                    hasCustomTitle: false,
                    templateContext,
                    movieTemplate,
                    episodeTemplate,
                    musicVideoTemplate,
                    songTemplate,
                    otherVideoTemplate,
                    minifier,
                    xml);

                currentStart = currentFinish;
                currentFinish += perItem;
            }
        }
    }

    private async Task WriteItemToXml(
        RefreshChannelData request,
        PlayoutItem displayItem,
        string start,
        string stop,
        bool hasCustomTitle,
        XmlTemplateContext templateContext,
        Template movieTemplate,
        Template episodeTemplate,
        Template musicVideoTemplate,
        Template songTemplate,
        Template otherVideoTemplate,
        XmlMinifier minifier,
        XmlWriter xml)
    {
        string title = GetTitle(displayItem);
        string subtitle = GetSubtitle(displayItem);

        Option<string> maybeTemplateOutput = displayItem.MediaItem switch
        {
            Movie templateMovie => await ProcessMovieTemplate(
                request,
                templateMovie,
                start,
                stop,
                hasCustomTitle,
                displayItem,
                title,
                templateContext,
                movieTemplate),
            Episode templateEpisode => await ProcessEpisodeTemplate(
                request,
                templateEpisode,
                start,
                stop,
                hasCustomTitle,
                displayItem,
                title,
                subtitle,
                templateContext,
                episodeTemplate),
            MusicVideo templateMusicVideo => await ProcessMusicVideoTemplate(
                request,
                templateMusicVideo,
                start,
                stop,
                hasCustomTitle,
                displayItem,
                title,
                subtitle,
                templateContext,
                musicVideoTemplate),
            Song templateSong => await ProcessSongTemplate(
                request,
                templateSong,
                start,
                stop,
                hasCustomTitle,
                displayItem,
                title,
                subtitle,
                templateContext,
                songTemplate),
            OtherVideo templateOtherVideo => await ProcessOtherVideoTemplate(
                request,
                templateOtherVideo,
                start,
                stop,
                hasCustomTitle,
                displayItem,
                title,
                templateContext,
                otherVideoTemplate),
            _ => Option<string>.None
        };

        foreach (string templateOutput in maybeTemplateOutput)
        {
            MarkupMinificationResult minified = minifier.Minify(templateOutput);
            await xml.WriteRawAsync(minified.MinifiedContent);
        }
    }

    private static async Task<Option<string>> ProcessMovieTemplate(
        RefreshChannelData request,
        Movie templateMovie,
        string start,
        string stop,
        bool hasCustomTitle,
        PlayoutItem displayItem,
        string title,
        XmlTemplateContext templateContext,
        Template movieTemplate)
    {
        foreach (MovieMetadata metadata in templateMovie.MovieMetadata.HeadOrNone())
        {
            metadata.Genres ??= [];
            metadata.Guids ??= [];
                    
            string poster = Optional(metadata.Artwork).Flatten()
                .Filter(a => a.ArtworkKind == ArtworkKind.Poster)
                .HeadOrNone()
                .Match(a => GetArtworkUrl(a, ArtworkKind.Poster), () => string.Empty);

            var data = new
            {
                ProgrammeStart = start,
                ProgrammeStop = stop,
                ChannelNumber = request.ChannelNumber,
                HasCustomTitle = hasCustomTitle,
                CustomTitle = displayItem.CustomTitle,
                MovieTitle = title,
                MovieHasPlot = !string.IsNullOrWhiteSpace(metadata.Plot),
                MoviePlot = metadata.Plot,
                MovieHasYear = metadata.Year.HasValue,
                MovieYear = metadata.Year,
                MovieGenres = metadata.Genres.Map(g => g.Name).OrderBy(n => n),
                MovieHasArtwork = !string.IsNullOrWhiteSpace(poster),
                MovieArtworkUrl = poster,
                MovieHasContentRating = !string.IsNullOrWhiteSpace(metadata.ContentRating),
                MovieContentRating = metadata.ContentRating,
                MovieGuids = metadata.Guids.Map(g => g.Guid)
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data);
            templateContext.PushGlobal(scriptObject);

            return await movieTemplate.RenderAsync(templateContext);
        }

        return Option<string>.None;
    }
    
    private static async Task<Option<string>> ProcessEpisodeTemplate(
        RefreshChannelData request,
        Episode templateEpisode,
        string start,
        string stop,
        bool hasCustomTitle,
        PlayoutItem displayItem,
        string title,
        string subtitle,
        XmlTemplateContext templateContext,
        Template episodeTemplate)
    {
        foreach (EpisodeMetadata metadata in templateEpisode.EpisodeMetadata.HeadOrNone())
        {
            metadata.Genres ??= [];
            metadata.Guids ??= [];

            foreach (ShowMetadata showMetadata in Optional(
                         templateEpisode.Season?.Show?.ShowMetadata.HeadOrNone()).Flatten())
            {
                showMetadata.Genres ??= [];
                showMetadata.Guids ??= [];

                string artworkPath = GetPrioritizedArtworkPath(metadata);

                var data = new
                {
                    ProgrammeStart = start,
                    ProgrammeStop = stop,
                    ChannelNumber = request.ChannelNumber,
                    HasCustomTitle = hasCustomTitle,
                    CustomTitle = displayItem.CustomTitle,
                    ShowTitle = title,
                    EpisodeHasTitle = !string.IsNullOrWhiteSpace(subtitle),
                    EpisodeTitle = subtitle,
                    EpisodeHasPlot = !string.IsNullOrWhiteSpace(metadata.Plot),
                    EpisodePlot = metadata.Plot,
                    ShowHasYear = showMetadata.Year.HasValue,
                    ShowYear = showMetadata.Year,
                    ShowGenres = showMetadata.Genres.Map(g => g.Name).OrderBy(n => n),
                    EpisodeHasArtwork = !string.IsNullOrWhiteSpace(artworkPath),
                    EpisodeArtworkUrl = artworkPath,
                    SeasonNumber = templateEpisode.Season?.SeasonNumber ?? 0,
                    EpisodeNumber = metadata.EpisodeNumber,
                    ShowHasContentRating = !string.IsNullOrWhiteSpace(showMetadata.ContentRating),
                    ShowContentRating = showMetadata.ContentRating,
                    ShowGuids = showMetadata.Guids.Map(g => g.Guid),
                    EpisodeGuids = metadata.Guids.Map(g => g.Guid)
                };
                
                var scriptObject = new ScriptObject();
                scriptObject.Import(data);
                templateContext.PushGlobal(scriptObject);

                return await episodeTemplate.RenderAsync(templateContext);
            }
        }

        return Option<string>.None;
    }

    private static async Task<Option<string>> ProcessMusicVideoTemplate(
        RefreshChannelData request,
        MusicVideo templateMusicVideo,
        string start,
        string stop,
        bool hasCustomTitle,
        PlayoutItem displayItem,
        string title,
        string subtitle,
        XmlTemplateContext templateContext,
        Template musicVideoTemplate)
    {
        foreach (MusicVideoMetadata metadata in templateMusicVideo.MusicVideoMetadata.HeadOrNone())
        {
            metadata.Genres ??= [];
            metadata.Artists ??= [];
            metadata.Studios ??= [];
            metadata.Directors ??= [];

            string artworkPath = GetPrioritizedArtworkPath(metadata);

            Option<ArtistMetadata> maybeMetadata =
                Optional(templateMusicVideo.Artist?.ArtistMetadata.HeadOrNone()).Flatten();

            var data = new
            {
                ProgrammeStart = start,
                ProgrammeStop = stop,
                ChannelNumber = request.ChannelNumber,
                HasCustomTitle = hasCustomTitle,
                CustomTitle = displayItem.CustomTitle,
                ArtistTitle = title,
                MusicVideoTitle = subtitle,
                MusicVideoHasPlot = !string.IsNullOrWhiteSpace(metadata.Plot),
                MusicVideoPlot = metadata.Plot,
                MusicVideoHasYear = metadata.Year.HasValue,
                MusicVideoYear = metadata.Year,
                MusicVideoGenres = metadata.Genres.Map(g => g.Name).OrderBy(n => n),
                ArtistGenres = maybeMetadata.SelectMany(m => m.Genres.Map(g => g.Name)).OrderBy(n => n),
                MusicVideoHasArtwork = !string.IsNullOrWhiteSpace(artworkPath),
                MusicVideoArtworkUrl = artworkPath,
                MusicVideoHasTrack = metadata.Track.HasValue,
                MusicVideoTrack = metadata.Track,
                MusicVideoHasAlbum = !string.IsNullOrWhiteSpace(metadata.Album),
                MusicVideoAlbum = metadata.Album,
                MusicVideoHasReleaseDate = metadata.ReleaseDate.HasValue,
                MusicVideoReleaseDate = metadata.ReleaseDate,
                MusicVideoAllArtists = metadata.Artists.Map(a => a.Name),
                MusicVideoStudios = metadata.Studios.Map(s => s.Name),
                MusicVideoDirectors = metadata.Directors.Map(d => d.Name)
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data);
            templateContext.PushGlobal(scriptObject);

            return await musicVideoTemplate.RenderAsync(templateContext);
        }

        return Option<string>.None;
    }
    
    private async Task<Option<string>> ProcessSongTemplate(
        RefreshChannelData request,
        Song templateSong,
        string start,
        string stop,
        bool hasCustomTitle,
        PlayoutItem displayItem,
        string title,
        string subtitle,
        XmlTemplateContext templateContext,
        Template songTemplate)
    {
        foreach (SongMetadata metadata in templateSong.SongMetadata.HeadOrNone())
        {
            metadata.Genres ??= [];
            metadata.Studios ??= [];

            string artworkPath = GetPrioritizedArtworkPath(metadata);

            var data = new
            {
                ProgrammeStart = start,
                ProgrammeStop = stop,
                ChannelNumber = request.ChannelNumber,
                HasCustomTitle = hasCustomTitle,
                CustomTitle = displayItem.CustomTitle,
                SongTitle = subtitle,
                SongArtists = metadata.Artists,
                SongAlbumArtists = metadata.AlbumArtists,
                SongHasYear = metadata.Year.HasValue,
                SongYear = metadata.Year,
                SongGenres = metadata.Genres.Map(g => g.Name).OrderBy(n => n),
                SongHasArtwork = !string.IsNullOrWhiteSpace(artworkPath),
                SongArtworkUrl = artworkPath,
                SongHasTrack = !string.IsNullOrWhiteSpace(metadata.Track),
                SongTrack = metadata.Track,
                SongHasComment = !string.IsNullOrWhiteSpace(metadata.Comment),
                SongComment = metadata.Comment,
                SongHasAlbum = !string.IsNullOrWhiteSpace(metadata.Album),
                SongAlbum = metadata.Album,
                SongHasReleaseDate = metadata.ReleaseDate.HasValue,
                SongReleaseDate = metadata.ReleaseDate,
                SongStudios = metadata.Studios.Map(s => s.Name),
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data);
            templateContext.PushGlobal(scriptObject);

            return await songTemplate.RenderAsync(templateContext);
        }

        return Option<string>.None;
    }
    
    private static async Task<Option<string>> ProcessOtherVideoTemplate(
        RefreshChannelData request,
        OtherVideo templateOtherVideo,
        string start,
        string stop,
        bool hasCustomTitle,
        PlayoutItem displayItem,
        string title,
        XmlTemplateContext templateContext,
        Template otherVideoTemplate)
    {
        foreach (OtherVideoMetadata metadata in templateOtherVideo.OtherVideoMetadata.HeadOrNone())
        {
            metadata.Genres ??= [];
            metadata.Guids ??= [];
                    
            var data = new
            {
                ProgrammeStart = start,
                ProgrammeStop = stop,
                ChannelNumber = request.ChannelNumber,
                HasCustomTitle = hasCustomTitle,
                CustomTitle = displayItem.CustomTitle,
                OtherVideoTitle = title,
                OtherVideoHasPlot = !string.IsNullOrWhiteSpace(metadata.Plot),
                OtherVideoPlot = metadata.Plot,
                OtherVideoHasYear = metadata.Year.HasValue,
                OtherVideoYear = metadata.Year,
                OtherVideoGenres = metadata.Genres.Map(g => g.Name).OrderBy(n => n),
                OtherVideoHasContentRating = !string.IsNullOrWhiteSpace(metadata.ContentRating),
                OtherVideoContentRating = metadata.ContentRating
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data);
            templateContext.PushGlobal(scriptObject);

            return await otherVideoTemplate.RenderAsync(templateContext);
        }

        return Option<string>.None;
    }

    private string GetMovieTemplateFileName()
    {
        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "movie.sbntxt");
        
        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_movie.sbntxt");
        }
        
        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate movie XMLTV fragment without template file {File}; please restart ErsatzTV",
                templateFileName);

            return null;
        }

        return templateFileName;
    }
    
    private string GetEpisodeTemplateFileName()
    {
        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "episode.sbntxt");
        
        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_episode.sbntxt");
        }
        
        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate episode XMLTV fragment without template file {File}; please restart ErsatzTV",
                templateFileName);

            return null;
        }

        return templateFileName;
    }
    
    private string GetMusicVideoTemplateFileName()
    {
        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "musicVideo.sbntxt");
        
        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_musicVideo.sbntxt");
        }
        
        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate music video XMLTV fragment without template file {File}; please restart ErsatzTV",
                templateFileName);

            return null;
        }

        return templateFileName;
    }
    
    private string GetSongTemplateFileName()
    {
        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "song.sbntxt");
        
        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_song.sbntxt");
        }
        
        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate song XMLTV fragment without template file {File}; please restart ErsatzTV",
                templateFileName);

            return null;
        }

        return templateFileName;
    }
    
    private string GetOtherVideoTemplateFileName()
    {
        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "otherVideo.sbntxt");
        
        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_otherVideo.sbntxt");
        }
        
        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate other video XMLTV fragment without template file {File}; please restart ErsatzTV",
                templateFileName);

            return null;
        }

        return templateFileName;
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
