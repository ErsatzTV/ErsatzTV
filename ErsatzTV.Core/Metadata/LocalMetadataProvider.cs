using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalMetadataProvider : ILocalMetadataProvider
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalMetadataProvider(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task RefreshMetadata(MediaItem mediaItem)
        {
            Option<MediaMetadata> maybeMetadata = await LoadMetadata(mediaItem);
            MediaMetadata metadata =
                maybeMetadata.IfNone(() => FallbackMetadataProvider.GetFallbackMetadata(mediaItem.Path));
            await ApplyMetadataUpdate(mediaItem, metadata);
        }

        private async Task ApplyMetadataUpdate(MediaItem mediaItem, MediaMetadata metadata)
        {
            if (mediaItem.Metadata == null)
            {
                mediaItem.Metadata = new MediaMetadata();
            }

            mediaItem.Metadata.MediaType = metadata.MediaType;
            mediaItem.Metadata.Title = metadata.Title;
            mediaItem.Metadata.Subtitle = metadata.Subtitle;
            mediaItem.Metadata.Description = metadata.Description;
            mediaItem.Metadata.EpisodeNumber = metadata.EpisodeNumber;
            mediaItem.Metadata.SeasonNumber = metadata.SeasonNumber;
            mediaItem.Metadata.Aired = metadata.Aired;
            mediaItem.Metadata.ContentRating = metadata.ContentRating;

            await _mediaItemRepository.Update(mediaItem);
        }

        private async Task<Option<MediaMetadata>> LoadMetadata(MediaItem mediaItem)
        {
            string nfoFileName = Path.ChangeExtension(mediaItem.Path, "nfo");
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                return None;
            }

            var tvShowSerializer = new XmlSerializer(typeof(TvShowEpisodeNfo));
            var movieSerializer = new XmlSerializer(typeof(MovieNfo));

            TryAsync<object> tvShowAttempt = TryAsync(
                async () =>
                {
                    await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open);
                    return tvShowSerializer.Deserialize(fileStream);
                });
            TryAsync<object> movieAttempt = TryAsync(
                async () =>
                {
                    await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open);
                    return movieSerializer.Deserialize(fileStream);
                });
            return await choice(tvShowAttempt, movieAttempt).Match<object, Option<MediaMetadata>>(
                result =>
                {
                    switch (result)
                    {
                        case TvShowEpisodeNfo nfo:
                            return new MediaMetadata
                            {
                                MediaType = MediaType.TvShow,
                                Title = nfo.ShowTitle,
                                Subtitle = nfo.Title,
                                Description = nfo.Outline,
                                EpisodeNumber = nfo.Episode,
                                SeasonNumber = nfo.Season,
                                Aired = GetAired(nfo.Aired)
                            };
                        case MovieNfo nfo:
                            return new MediaMetadata
                            {
                                MediaType = MediaType.Movie,
                                Title = nfo.Title,
                                Description = nfo.Outline,
                                ContentRating = nfo.ContentRating,
                                Aired = GetAired(nfo.Premiered)
                            };
                        default:
                            return None;
                    }
                },
                None);
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
