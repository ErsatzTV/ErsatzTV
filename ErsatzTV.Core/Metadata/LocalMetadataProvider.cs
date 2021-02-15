using System;
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
        private static readonly XmlSerializer TvShowSerializer = new(typeof(TvShowEpisodeNfo));
        private readonly ILogger<LocalMetadataProvider> _logger;

        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalMetadataProvider(IMediaItemRepository mediaItemRepository, ILogger<LocalMetadataProvider> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _logger = logger;
        }

        public async Task RefreshSidecarMetadata(MediaItem mediaItem, string path)
        {
            Option<MediaMetadata> maybeMetadata = await LoadMetadata(mediaItem, path);
            await maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(mediaItem, metadata));
        }

        public Task RefreshFallbackMetadata(MediaItem mediaItem) =>
            ApplyMetadataUpdate(mediaItem, FallbackMetadataProvider.GetFallbackMetadata(mediaItem));

        private async Task ApplyMetadataUpdate(MediaItem mediaItem, MediaMetadata metadata)
        {
            if (mediaItem.Metadata == null)
            {
                mediaItem.Metadata = new MediaMetadata();
            }

            mediaItem.Metadata.Source = metadata.Source;
            mediaItem.Metadata.LastWriteTime = metadata.LastWriteTime;
            mediaItem.Metadata.MediaType = metadata.MediaType;
            mediaItem.Metadata.Title = metadata.Title;
            mediaItem.Metadata.Subtitle = metadata.Subtitle;
            mediaItem.Metadata.SortTitle =
                (metadata.Title ?? string.Empty).ToLowerInvariant().StartsWith("the ")
                    ? metadata.Title?.Substring(4)
                    : metadata.Title;
            mediaItem.Metadata.Description = metadata.Description;
            mediaItem.Metadata.EpisodeNumber = metadata.EpisodeNumber;
            mediaItem.Metadata.SeasonNumber = metadata.SeasonNumber;
            mediaItem.Metadata.Aired = metadata.Aired;
            mediaItem.Metadata.ContentRating = metadata.ContentRating;

            await _mediaItemRepository.Update(mediaItem);
        }

        private async Task<Option<MediaMetadata>> LoadMetadata(MediaItem mediaItem, string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            if (!(mediaItem.Source is LocalMediaSource localMediaSource))
            {
                _logger.LogDebug("Media source {Name} is not a local media source", mediaItem.Source.Name);
                return None;
            }

            return localMediaSource.MediaType switch
            {
                MediaType.Movie => await LoadMovieMetadata(nfoFileName),
                MediaType.TvShow => await LoadTvShowMetadata(nfoFileName),
                _ => None
            };
        }

        private async Task<Option<MediaMetadata>> LoadTvShowMetadata(string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowEpisodeNfo> maybeNfo = TvShowSerializer.Deserialize(fileStream) as TvShowEpisodeNfo;
                return maybeNfo.Match<Option<MediaMetadata>>(
                    nfo => new MediaMetadata
                    {
                        Source = MetadataSource.Sidecar,
                        LastWriteTime = File.GetLastWriteTimeUtc(nfoFileName),
                        MediaType = MediaType.TvShow,
                        Title = nfo.ShowTitle,
                        Subtitle = nfo.Title,
                        Description = nfo.Outline,
                        EpisodeNumber = nfo.Episode,
                        SeasonNumber = nfo.Season,
                        Aired = GetAired(nfo.Aired)
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read TV nfo metadata from {Path}", nfoFileName);
                return None;
            }
        }

        private async Task<Option<MediaMetadata>> LoadMovieMetadata(string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<MovieNfo> maybeNfo = MovieSerializer.Deserialize(fileStream) as MovieNfo;
                return maybeNfo.Match<Option<MediaMetadata>>(
                    nfo => new MediaMetadata
                    {
                        Source = MetadataSource.Sidecar,
                        LastWriteTime = File.GetLastWriteTimeUtc(nfoFileName),
                        MediaType = MediaType.Movie,
                        Title = nfo.Title,
                        Description = nfo.Outline,
                        ContentRating = nfo.ContentRating,
                        Aired = GetAired(nfo.Premiered)
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read Movie nfo metadata from {Path}", nfoFileName);
                return None;
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

        [XmlRoot("movie")]
        public class MovieNfo
        {
            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("outline")]
            public string Outline { get; set; }

            [XmlElement("mpaa")]
            public string ContentRating { get; set; }

            [XmlElement("premiered")]
            public string Premiered { get; set; }
        }

        [XmlRoot("episodedetails")]
        public class TvShowEpisodeNfo
        {
            [XmlElement("showtitle")]
            public string ShowTitle { get; set; }

            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("outline")]
            public string Outline { get; set; }

            [XmlElement("episode")]
            public int Episode { get; set; }

            [XmlElement("season")]
            public int Season { get; set; }

            [XmlElement("mpaa")]
            public string ContentRating { get; set; }

            [XmlElement("aired")]
            public string Aired { get; set; }
        }
    }
}
