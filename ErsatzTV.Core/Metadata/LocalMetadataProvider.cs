using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalMetadataProvider : ILocalMetadataProvider
    {
        private static readonly XmlSerializer MovieSerializer = new(typeof(MovieNfo));
        private static readonly XmlSerializer EpisodeSerializer = new(typeof(TvShowEpisodeNfo));
        private static readonly XmlSerializer TvShowSerializer = new(typeof(TvShowNfo));
        private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<LocalMetadataProvider> _logger;

        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public LocalMetadataProvider(
            IMediaItemRepository mediaItemRepository,
            ITelevisionRepository televisionRepository,
            IFallbackMetadataProvider fallbackMetadataProvider,
            ILocalFileSystem localFileSystem,
            ILogger<LocalMetadataProvider> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _televisionRepository = televisionRepository;
            _fallbackMetadataProvider = fallbackMetadataProvider;
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public Task<TelevisionShowMetadata> GetMetadataForShow(string showFolder)
        {
            string nfoFileName = Path.Combine(showFolder, "tvshow.nfo");
            return Optional(_localFileSystem.FileExists(nfoFileName))
                .Filter(identity).AsTask()
                .Bind(_ => LoadTelevisionShowMetadata(nfoFileName))
                .IfNoneAsync(() => _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder).AsTask())
                .Map(
                    m =>
                    {
                        m.SortTitle = GetSortTitle(m.Title);
                        return m;
                    });
        }

        public Task<Unit> RefreshSidecarMetadata(MediaItem mediaItem, string path) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => LoadMetadata(e, path)
                    .Bind(maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(e, metadata))),
                Movie m => LoadMetadata(m, path)
                    .Bind(maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(m, metadata))),
                _ => Task.FromResult(Unit.Default)
            };

        public Task<Unit> RefreshSidecarMetadata(TelevisionShow televisionShow, string showFolder) =>
            LoadMetadata(televisionShow, showFolder).Bind(
                maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(televisionShow, metadata)));

        public Task<Unit> RefreshFallbackMetadata(MediaItem mediaItem) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => ApplyMetadataUpdate(e, FallbackMetadataProvider.GetFallbackMetadata(e))
                    .ToUnit(),
                Movie m => ApplyMetadataUpdate(m, FallbackMetadataProvider.GetFallbackMetadata(m)).ToUnit(),
                _ => Task.FromResult(Unit.Default)
            };

        public Task<Unit> RefreshFallbackMetadata(TelevisionShow televisionShow, string showFolder) =>
            ApplyMetadataUpdate(televisionShow, _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder))
                .ToUnit();

        private async Task ApplyMetadataUpdate(TelevisionEpisodeMediaItem mediaItem, TelevisionEpisodeMetadata metadata)
        {
            mediaItem.Metadata ??= new TelevisionEpisodeMetadata { TelevisionEpisodeId = mediaItem.Id };
            mediaItem.Metadata.Source = metadata.Source;
            mediaItem.Metadata.LastWriteTime = metadata.LastWriteTime;
            mediaItem.Metadata.Title = metadata.Title;
            mediaItem.Metadata.SortTitle = GetSortTitle(metadata.Title);
            mediaItem.Metadata.Season = metadata.Season;
            mediaItem.Metadata.Episode = metadata.Episode;
            mediaItem.Metadata.Plot = metadata.Plot;
            mediaItem.Metadata.Aired = metadata.Aired;

            await _televisionRepository.Update(mediaItem);
        }

        private async Task ApplyMetadataUpdate(Movie movie, MovieMetadata metadata)
        {
            movie.MovieMetadata = new List<MovieMetadata> { metadata };
            await _mediaItemRepository.Update(movie);
        }

        private async Task ApplyMetadataUpdate(TelevisionShow televisionShow, TelevisionShowMetadata metadata)
        {
            televisionShow.Metadata ??= new TelevisionShowMetadata();
            televisionShow.Metadata.Source = metadata.Source;
            televisionShow.Metadata.LastWriteTime = metadata.LastWriteTime;
            televisionShow.Metadata.Title = metadata.Title;
            televisionShow.Metadata.Plot = metadata.Plot;
            televisionShow.Metadata.Year = metadata.Year;
            televisionShow.Metadata.SortTitle = GetSortTitle(metadata.Title);

            await _televisionRepository.Update(televisionShow);
        }

        private async Task<Option<MovieMetadata>> LoadMetadata(Movie mediaItem, string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            // if (!(mediaItem.Source is LocalMediaSource))
            // {
            //     _logger.LogDebug("Media source {Name} is not a local media source", mediaItem.Source.Name);
            //     return None;
            // }

            return await LoadMovieMetadata(mediaItem, nfoFileName);
        }

        private async Task<Option<TelevisionEpisodeMetadata>> LoadMetadata(
            TelevisionEpisodeMediaItem mediaItem,
            string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            // if (!(mediaItem.Source is LocalMediaSource))
            // {
            //     _logger.LogDebug("Media source {Name} is not a local media source", mediaItem.Source.Name);
            //     return None;
            // }

            return await LoadEpisodeMetadata(mediaItem, nfoFileName);
        }

        private async Task<Option<TelevisionShowMetadata>> LoadMetadata(
            TelevisionShow televisionShow,
            string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            return await LoadTelevisionShowMetadata(nfoFileName);
        }

        private async Task<Option<TelevisionShowMetadata>> LoadTelevisionShowMetadata(string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowNfo> maybeNfo = TvShowSerializer.Deserialize(fileStream) as TvShowNfo;
                return maybeNfo.Match<Option<TelevisionShowMetadata>>(
                    nfo => new TelevisionShowMetadata
                    {
                        Source = MetadataSource.Sidecar,
                        LastWriteTime = File.GetLastWriteTimeUtc(nfoFileName),
                        Title = nfo.Title,
                        Plot = nfo.Plot,
                        Year = nfo.Year
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read TV show nfo metadata from {Path}", nfoFileName);
                return None;
            }
        }

        private async Task<Option<TelevisionEpisodeMetadata>> LoadEpisodeMetadata(
            TelevisionEpisodeMediaItem mediaItem,
            string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowEpisodeNfo> maybeNfo = EpisodeSerializer.Deserialize(fileStream) as TvShowEpisodeNfo;
                return maybeNfo.Match<Option<TelevisionEpisodeMetadata>>(
                    nfo => new TelevisionEpisodeMetadata
                    {
                        Source = MetadataSource.Sidecar,
                        LastWriteTime = File.GetLastWriteTimeUtc(nfoFileName),
                        Title = nfo.Title,
                        Aired = GetAired(nfo.Aired),
                        Episode = nfo.Episode,
                        Season = nfo.Season,
                        Plot = nfo.Plot
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read TV episode nfo metadata from {Path}", nfoFileName);
                return FallbackMetadataProvider.GetFallbackMetadata(mediaItem);
            }
        }

        private async Task<Option<MovieMetadata>> LoadMovieMetadata(Movie mediaItem, string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<MovieNfo> maybeNfo = MovieSerializer.Deserialize(fileStream) as MovieNfo;
                return maybeNfo.Match<Option<MovieMetadata>>(
                    nfo => new MovieMetadata
                    {
                        MetadataKind = MetadataKind.Sidecar,
                        DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                        Title = nfo.Title,
                        ReleaseDate = nfo.Premiered,
                        Plot = nfo.Plot,
                        Outline = nfo.Outline,
                        Tagline = nfo.Tagline,
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read Movie nfo metadata from {Path}", nfoFileName);
                return FallbackMetadataProvider.GetFallbackMetadata(mediaItem);
            }
        }

        private static DateTime? GetAired(string aired)
        {
            if (string.IsNullOrWhiteSpace(aired))
            {
                return null;
            }

            if (DateTime.TryParse(aired, out DateTime parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string GetSortTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            if (title.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                return title.Substring(4);
            }

            if (title.StartsWith("Æ"))
            {
                return title.Replace("Æ", "E");
            }

            return title;
        }

        [XmlRoot("movie")]
        public class MovieNfo
        {
            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("outline")]
            public string Outline { get; set; }

            [XmlElement("year")]
            public int Year { get; set; }

            [XmlElement("mpaa")]
            public string ContentRating { get; set; }

            [XmlElement("premiered")]
            public DateTime Premiered { get; set; }

            [XmlElement("plot")]
            public string Plot { get; set; }

            [XmlElement("tagline")]
            public string Tagline { get; set; }
        }

        [XmlRoot("tvshow")]
        public class TvShowNfo
        {
            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("year")]
            public int Year { get; set; }

            [XmlElement("plot")]
            public string Plot { get; set; }
        }

        [XmlRoot("episodedetails")]
        public class TvShowEpisodeNfo
        {
            [XmlElement("showtitle")]
            public string ShowTitle { get; set; }

            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("episode")]
            public int Episode { get; set; }

            [XmlElement("season")]
            public int Season { get; set; }

            [XmlElement("mpaa")]
            public string ContentRating { get; set; }

            [XmlElement("aired")]
            public string Aired { get; set; }

            [XmlElement("plot")]
            public string Plot { get; set; }
        }
    }
}
