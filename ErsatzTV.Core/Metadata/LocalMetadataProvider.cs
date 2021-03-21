using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly IMetadataRepository _metadataRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public LocalMetadataProvider(
            IMetadataRepository metadataRepository,
            ITelevisionRepository televisionRepository,
            IFallbackMetadataProvider fallbackMetadataProvider,
            ILocalFileSystem localFileSystem,
            ILogger<LocalMetadataProvider> logger)
        {
            _metadataRepository = metadataRepository;
            _televisionRepository = televisionRepository;
            _fallbackMetadataProvider = fallbackMetadataProvider;
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public async Task<ShowMetadata> GetMetadataForShow(string showFolder)
        {
            string nfoFileName = Path.Combine(showFolder, "tvshow.nfo");
            Option<ShowMetadata> maybeMetadata = None;
            if (_localFileSystem.FileExists(nfoFileName))
            {
                maybeMetadata = await LoadTelevisionShowMetadata(nfoFileName);
            }

            return maybeMetadata.Match(
                metadata =>
                {
                    metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                    return metadata;
                },
                () =>
                {
                    ShowMetadata metadata = _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder);
                    metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                    return metadata;
                });
        }

        public Task<bool> RefreshSidecarMetadata(MediaItem mediaItem, string path) =>
            mediaItem switch
            {
                Episode e => LoadMetadata(e, path)
                    .Bind(
                        maybeMetadata => maybeMetadata.Match(
                            metadata => ApplyMetadataUpdate(e, metadata),
                            () => Task.FromResult(false))),
                Movie m => LoadMetadata(m, path)
                    .Bind(
                        maybeMetadata => maybeMetadata.Match(
                            metadata => ApplyMetadataUpdate(m, metadata),
                            () => Task.FromResult(false))),
                _ => Task.FromResult(false)
            };

        public Task<bool> RefreshSidecarMetadata(Show televisionShow, string showFolder) =>
            LoadMetadata(televisionShow, showFolder).Bind(
                maybeMetadata => maybeMetadata.Match(
                    metadata => ApplyMetadataUpdate(televisionShow, metadata),
                    () => Task.FromResult(false)));

        public Task<bool> RefreshFallbackMetadata(MediaItem mediaItem) =>
            mediaItem switch
            {
                Episode e => ApplyMetadataUpdate(e, _fallbackMetadataProvider.GetFallbackMetadata(e)),
                Movie m => ApplyMetadataUpdate(m, _fallbackMetadataProvider.GetFallbackMetadata(m)),
                _ => Task.FromResult(false)
            };

        public Task<bool> RefreshFallbackMetadata(Show televisionShow, string showFolder) =>
            ApplyMetadataUpdate(televisionShow, _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder));

        private async Task<bool> ApplyMetadataUpdate(Episode episode, Tuple<EpisodeMetadata, int> metadataEpisodeNumber)
        {
            (EpisodeMetadata metadata, int episodeNumber) = metadataEpisodeNumber;
            if (episode.EpisodeNumber != episodeNumber)
            {
                await _televisionRepository.SetEpisodeNumber(episode, episodeNumber);
            }

            await Optional(episode.EpisodeMetadata).Flatten().HeadOrNone().Match(
                existing =>
                {
                    existing.Outline = metadata.Outline;
                    existing.Plot = metadata.Plot;
                    existing.Tagline = metadata.Tagline;
                    existing.Title = metadata.Title;
                    existing.DateAdded = metadata.DateAdded;
                    existing.DateUpdated = metadata.DateUpdated;
                    existing.MetadataKind = metadata.MetadataKind;
                    existing.OriginalTitle = metadata.OriginalTitle;
                    existing.ReleaseDate = metadata.ReleaseDate;
                    existing.Year = metadata.Year;
                    existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;

                    return _metadataRepository.Update(existing);
                },
                () =>
                {
                    metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;
                    metadata.EpisodeId = episode.Id;
                    episode.EpisodeMetadata = new List<EpisodeMetadata> { metadata };

                    return _metadataRepository.Add(metadata);
                });

            return true;
        }

        private Task<bool> ApplyMetadataUpdate(Movie movie, MovieMetadata metadata) =>
            Optional(movie.MovieMetadata).Flatten().HeadOrNone().Match(
                existing =>
                {
                    existing.Outline = metadata.Outline;
                    existing.Plot = metadata.Plot;
                    existing.Tagline = metadata.Tagline;
                    existing.Title = metadata.Title;
                    existing.DateAdded = metadata.DateAdded;
                    existing.DateUpdated = metadata.DateUpdated;
                    existing.MetadataKind = metadata.MetadataKind;
                    existing.OriginalTitle = metadata.OriginalTitle;
                    existing.ReleaseDate = metadata.ReleaseDate;
                    existing.Year = metadata.Year;
                    existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;

                    foreach (Genre genre in existing.Genres.Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                        .ToList())
                    {
                        existing.Genres.Remove(genre);
                    }

                    foreach (Genre genre in metadata.Genres.Filter(g => existing.Genres.All(g2 => g2.Name != g.Name))
                        .ToList())
                    {
                        existing.Genres.Add(genre);
                    }

                    foreach (Tag tag in existing.Tags.Filter(t => metadata.Tags.All(t2 => t2.Name != t.Name))
                        .ToList())
                    {
                        existing.Tags.Remove(tag);
                    }

                    foreach (Tag tag in metadata.Tags.Filter(t => existing.Tags.All(t2 => t2.Name != t.Name))
                        .ToList())
                    {
                        existing.Tags.Add(tag);
                    }

                    return _metadataRepository.Update(existing);
                },
                () =>
                {
                    metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;
                    metadata.MovieId = movie.Id;
                    movie.MovieMetadata = new List<MovieMetadata> { metadata };

                    return _metadataRepository.Add(metadata);
                });

        private Task<bool> ApplyMetadataUpdate(Show show, ShowMetadata metadata) =>
            Optional(show.ShowMetadata).Flatten().HeadOrNone().Match(
                existing =>
                {
                    existing.Outline = metadata.Outline;
                    existing.Plot = metadata.Plot;
                    existing.Tagline = metadata.Tagline;
                    existing.Title = metadata.Title;
                    existing.DateAdded = metadata.DateAdded;
                    existing.DateUpdated = metadata.DateUpdated;
                    existing.MetadataKind = metadata.MetadataKind;
                    existing.OriginalTitle = metadata.OriginalTitle;
                    existing.ReleaseDate = metadata.ReleaseDate;
                    existing.Year = metadata.Year;
                    existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;

                    foreach (Genre genre in existing.Genres.Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                        .ToList())
                    {
                        existing.Genres.Remove(genre);
                    }

                    foreach (Genre genre in metadata.Genres.Filter(g => existing.Genres.All(g2 => g2.Name != g.Name))
                        .ToList())
                    {
                        existing.Genres.Add(genre);
                    }

                    foreach (Tag tag in existing.Tags.Filter(t => metadata.Tags.All(t2 => t2.Name != t.Name))
                        .ToList())
                    {
                        existing.Tags.Remove(tag);
                    }

                    foreach (Tag tag in metadata.Tags.Filter(t => existing.Tags.All(t2 => t2.Name != t.Name))
                        .ToList())
                    {
                        existing.Tags.Add(tag);
                    }

                    return _metadataRepository.Update(existing);
                },
                () =>
                {
                    metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;
                    metadata.ShowId = show.Id;
                    show.ShowMetadata = new List<ShowMetadata> { metadata };

                    return _metadataRepository.Add(metadata);
                });

        private async Task<Option<MovieMetadata>> LoadMetadata(Movie mediaItem, string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            return await LoadMovieMetadata(mediaItem, nfoFileName);
        }

        private async Task<Option<Tuple<EpisodeMetadata, int>>> LoadMetadata(Episode mediaItem, string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            return await LoadEpisodeMetadata(mediaItem, nfoFileName);
        }

        private async Task<Option<ShowMetadata>> LoadMetadata(Show televisionShow, string nfoFileName)
        {
            if (nfoFileName == null || !File.Exists(nfoFileName))
            {
                _logger.LogDebug("NFO file does not exist at {Path}", nfoFileName);
                return None;
            }

            return await LoadTelevisionShowMetadata(nfoFileName);
        }

        private async Task<Option<ShowMetadata>> LoadTelevisionShowMetadata(string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowNfo> maybeNfo = TvShowSerializer.Deserialize(fileStream) as TvShowNfo;
                return maybeNfo.Match<Option<ShowMetadata>>(
                    nfo => new ShowMetadata
                    {
                        MetadataKind = MetadataKind.Sidecar,
                        DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                        Title = nfo.Title,
                        Plot = nfo.Plot,
                        Outline = nfo.Outline,
                        Tagline = nfo.Tagline,
                        Year = GetYear(nfo.Year, nfo.Premiered),
                        ReleaseDate = GetAired(nfo.Year, nfo.Premiered),
                        Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                        Tags = nfo.Tags.Map(t => new Tag { Name = t }).ToList()
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to read TV show nfo metadata from {Path}", nfoFileName);
                return None;
            }
        }

        private async Task<Option<Tuple<EpisodeMetadata, int>>> LoadEpisodeMetadata(Episode episode, string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowEpisodeNfo> maybeNfo = EpisodeSerializer.Deserialize(fileStream) as TvShowEpisodeNfo;
                return maybeNfo.Match<Option<Tuple<EpisodeMetadata, int>>>(
                    nfo =>
                    {
                        var metadata = new EpisodeMetadata
                        {
                            MetadataKind = MetadataKind.Sidecar,
                            DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                            Title = nfo.Title,
                            ReleaseDate = GetAired(0, nfo.Aired),
                            Plot = nfo.Plot
                        };
                        return Tuple(metadata, nfo.Episode);
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to read TV episode nfo metadata from {Path}", nfoFileName);
                return _fallbackMetadataProvider.GetFallbackMetadata(episode);
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
                        Year = nfo.Year,
                        ReleaseDate = nfo.Premiered,
                        Plot = nfo.Plot,
                        Outline = nfo.Outline,
                        Tagline = nfo.Tagline,
                        Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                        Tags = nfo.Tags.Map(t => new Tag { Name = t }).ToList()
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to read Movie nfo metadata from {Path}", nfoFileName);
                return _fallbackMetadataProvider.GetFallbackMetadata(mediaItem);
            }
        }

        private static int? GetYear(int year, string premiered)
        {
            if (year > 1000)
            {
                return year;
            }

            if (string.IsNullOrWhiteSpace(premiered))
            {
                return null;
            }

            if (DateTime.TryParse(premiered, out DateTime parsed))
            {
                return parsed.Year;
            }

            return null;
        }

        private static DateTime? GetAired(int year, string aired)
        {
            DateTime? fallback = year > 1000 ? new DateTime(year, 1, 1) : null;

            if (string.IsNullOrWhiteSpace(aired))
            {
                return fallback;
            }

            return DateTime.TryParse(aired, out DateTime parsed) ? parsed : fallback;
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

            [XmlElement("genre")]
            public List<string> Genres { get; set; }

            [XmlElement("tag")]
            public List<string> Tags { get; set; }
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

            [XmlElement("outline")]
            public string Outline { get; set; }

            [XmlElement("tagline")]
            public string Tagline { get; set; }

            [XmlElement("premiered")]
            public string Premiered { get; set; }

            [XmlElement("genre")]
            public List<string> Genres { get; set; }

            [XmlElement("tag")]
            public List<string> Tags { get; set; }
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
