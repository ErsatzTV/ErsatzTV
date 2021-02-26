﻿using System;
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

        public Task<ShowMetadata> GetMetadataForShow(string showFolder)
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
                Episode e => LoadMetadata(e, path)
                    .Bind(maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(e, metadata))),
                Movie m => LoadMetadata(m, path)
                    .Bind(maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(m, metadata))),
                _ => Task.FromResult(Unit.Default)
            };

        public Task<Unit> RefreshSidecarMetadata(Show televisionShow, string showFolder) =>
            LoadMetadata(televisionShow, showFolder).Bind(
                maybeMetadata => maybeMetadata.IfSomeAsync(metadata => ApplyMetadataUpdate(televisionShow, metadata)));

        public Task<Unit> RefreshFallbackMetadata(MediaItem mediaItem) =>
            mediaItem switch
            {
                Episode e => ApplyMetadataUpdate(e, FallbackMetadataProvider.GetFallbackMetadata(e))
                    .ToUnit(),
                Movie m => ApplyMetadataUpdate(m, FallbackMetadataProvider.GetFallbackMetadata(m)).ToUnit(),
                _ => Task.FromResult(Unit.Default)
            };

        public Task<Unit> RefreshFallbackMetadata(Show televisionShow, string showFolder) =>
            ApplyMetadataUpdate(televisionShow, _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder))
                .ToUnit();

        private async Task ApplyMetadataUpdate(Episode episode, EpisodeMetadata metadata)
        {
            episode.EpisodeMetadata.HeadOrNone().Match(
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
                    existing.SortTitle = metadata.SortTitle ?? GetSortTitle(metadata.Title);
                },
                () => episode.EpisodeMetadata = new List<EpisodeMetadata> { metadata });

            await _televisionRepository.Update(episode);
        }

        private async Task ApplyMetadataUpdate(Movie movie, MovieMetadata metadata)
        {
            movie.MovieMetadata.HeadOrNone().Match(
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
                    existing.SortTitle = metadata.SortTitle ?? GetSortTitle(metadata.Title);
                },
                () => movie.MovieMetadata = new List<MovieMetadata> { metadata });

            await _mediaItemRepository.Update(movie);
        }

        private async Task ApplyMetadataUpdate(Show show, ShowMetadata metadata)
        {
            show.ShowMetadata.HeadOrNone().Match(
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
                    existing.SortTitle = metadata.SortTitle ?? GetSortTitle(metadata.Title);
                },
                () => show.ShowMetadata = new List<ShowMetadata> { metadata });

            await _televisionRepository.Update(show);
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

        private async Task<Option<EpisodeMetadata>> LoadMetadata(Episode mediaItem, string nfoFileName)
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

        private async Task<Option<ShowMetadata>> LoadMetadata(
            Show televisionShow,
            string nfoFileName)
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
                        ReleaseDate = new DateTime(nfo.Year, 1, 1)
                    },
                    None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read TV show nfo metadata from {Path}", nfoFileName);
                return None;
            }
        }

        private async Task<Option<EpisodeMetadata>> LoadEpisodeMetadata(
            Episode mediaItem,
            string nfoFileName)
        {
            try
            {
                await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
                Option<TvShowEpisodeNfo> maybeNfo = EpisodeSerializer.Deserialize(fileStream) as TvShowEpisodeNfo;
                return maybeNfo.Match<Option<EpisodeMetadata>>(
                    nfo => new EpisodeMetadata
                    {
                        MetadataKind = MetadataKind.Sidecar,
                        DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                        Title = nfo.Title,
                        ReleaseDate = GetAired(nfo.Aired),
                        // Episode = nfo.Episode,
                        // Season = nfo.Season,
                        // TODO: save episode number somewhere?
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
                        Tagline = nfo.Tagline
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
