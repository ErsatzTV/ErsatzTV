using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Plex.Models;
using LanguageExt;
using Refit;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Plex
{
    public class PlexServerApiClient : IPlexServerApiClient
    {
        private readonly IFallbackMetadataProvider _fallbackMetadataProvider;

        public PlexServerApiClient(IFallbackMetadataProvider fallbackMetadataProvider) =>
            _fallbackMetadataProvider = fallbackMetadataProvider;

        public async Task<Either<BaseError, List<PlexLibrary>>> GetLibraries(
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                List<PlexLibraryResponse> directory =
                    await service.GetLibraries(token.AuthToken).Map(r => r.MediaContainer.Directory);
                return directory
                    // .Filter(l => l.Hidden == 0)
                    .Map(Project)
                    .Somes()
                    .ToList();
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<PlexMovie>>> GetMovieLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetLibrarySectionContents(library.Key, token.AuthToken)
                    .Map(r => r.MediaContainer.Metadata.Filter(m => m.Media.Count > 0 && m.Media[0].Part.Count > 0))
                    .Map(list => list.Map(metadata => ProjectToMovie(metadata, library.MediaSourceId)).ToList());
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<PlexShow>>> GetShowLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetLibrarySectionContents(library.Key, token.AuthToken)
                    .Map(r => r.MediaContainer.Metadata)
                    .Map(list => list.Map(metadata => ProjectToShow(metadata, library.MediaSourceId)).ToList());
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<PlexSeason>>> GetShowSeasons(
            PlexLibrary library,
            PlexShow show,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetChildren(show.Key.Split("/").Reverse().Skip(1).Head(), token.AuthToken)
                    .Map(r => r.MediaContainer.Metadata)
                    .Map(list => list.Map(metadata => ProjectToSeason(metadata, library.MediaSourceId)).ToList());
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<PlexEpisode>>> GetSeasonEpisodes(
            PlexLibrary library,
            PlexSeason season,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetChildren(season.Key.Split("/").Reverse().Skip(1).Head(), token.AuthToken)
                    .Map(r => r.MediaContainer.Metadata.Filter(m => m.Media.Count > 0 && m.Media[0].Part.Count > 0))
                    .Map(list => list.Map(metadata => ProjectToEpisode(metadata, library.MediaSourceId)).ToList());
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, MediaVersion>> GetStatistics(
            string key,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetMetadata(key, token.AuthToken)
                    .Map(
                        r => r.MediaContainer.Metadata.Filter(m => m.Media.Count > 0 && m.Media[0].Part.Count > 0)
                            .HeadOrNone())
                    .BindT(ProjectToMediaVersion)
                    .Map(o => o.ToEither<BaseError>("Unable to locate metadata"));
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static Option<PlexLibrary> Project(PlexLibraryResponse response) =>
            response.Type switch
            {
                "show" => new PlexLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaKind = LibraryMediaKind.Shows,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"plex://{response.Uuid}" } }
                },
                "movie" => new PlexLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaKind = LibraryMediaKind.Movies,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"plex://{response.Uuid}" } }
                },
                // TODO: "artist" for music libraries
                _ => None
            };

        private PlexMovie ProjectToMovie(PlexMetadataResponse response, int mediaSourceId)
        {
            PlexMediaResponse media = response.Media.Head();
            PlexPartResponse part = media.Part.Head();
            DateTime dateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime;
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new MovieMetadata
            {
                Title = response.Title,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(response.Title),
                Plot = response.Summary,
                Year = response.Year,
                Tagline = response.Tagline,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime,
                Genres = Optional(response.Genre).Flatten().Map(g => new Genre { Name = g.Tag }).ToList(),
                Tags = new List<Tag>(),
                Studios = new List<Studio>()
            };

            if (!string.IsNullOrWhiteSpace(response.Studio))
            {
                metadata.Studios.Add(new Studio { Name = response.Studio });
            }

            if (DateTime.TryParse(response.OriginallyAvailableAt, out DateTime releaseDate))
            {
                metadata.ReleaseDate = releaseDate;
            }

            if (!string.IsNullOrWhiteSpace(response.Thumb))
            {
                var path = $"plex/{mediaSourceId}{response.Thumb}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            if (!string.IsNullOrWhiteSpace(response.Art))
            {
                var path = $"plex/{mediaSourceId}{response.Art}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            var version = new MediaVersion
            {
                Name = "Main",
                Duration = TimeSpan.FromMilliseconds(media.Duration),
                Width = media.Width,
                Height = media.Height,
                // specifically omit sample aspect ratio
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime,
                MediaFiles = new List<MediaFile>
                {
                    new PlexMediaFile
                    {
                        PlexId = part.Id,
                        Key = part.Key,
                        Path = part.File
                    }
                },
                Streams = new List<MediaStream>()
            };

            var movie = new PlexMovie
            {
                Key = response.Key,
                MovieMetadata = new List<MovieMetadata> { metadata },
                MediaVersions = new List<MediaVersion> { version }
            };

            return movie;
        }

        private Option<MediaVersion> ProjectToMediaVersion(PlexMetadataResponse response)
        {
            List<PlexStreamResponse> streams = response.Media.Head().Part.Head().Stream;
            DateTime dateUpdated = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;
            Option<PlexStreamResponse> maybeVideoStream = streams.Find(s => s.StreamType == 1);
            return maybeVideoStream.Map(
                videoStream =>
                {
                    var version = new MediaVersion
                    {
                        SampleAspectRatio = videoStream.PixelAspectRatio ?? "1:1",
                        VideoScanKind = videoStream.ScanType switch
                        {
                            "interlaced" => VideoScanKind.Interlaced,
                            "progressive" => VideoScanKind.Progressive,
                            _ => VideoScanKind.Unknown
                        },
                        Streams = new List<MediaStream>(),
                        DateUpdated = dateUpdated
                    };

                    version.Streams.Add(
                        new MediaStream
                        {
                            MediaStreamKind = MediaStreamKind.Video,
                            Index = videoStream.Index,
                            Codec = videoStream.Codec,
                            Profile = (videoStream.Profile ?? string.Empty).ToLowerInvariant(),
                            Default = videoStream.Default,
                            Language = videoStream.LanguageCode,
                            Forced = videoStream.Forced
                        });

                    foreach (PlexStreamResponse audioStream in streams.Filter(s => s.StreamType == 2))
                    {
                        var stream = new MediaStream
                        {
                            MediaVersionId = version.Id,
                            MediaStreamKind = MediaStreamKind.Audio,
                            Index = audioStream.Index,
                            Codec = audioStream.Codec,
                            Profile = (audioStream.Profile ?? string.Empty).ToLowerInvariant(),
                            Channels = audioStream.Channels,
                            Default = audioStream.Default,
                            Forced = audioStream.Forced,
                            Language = audioStream.LanguageCode
                        };

                        version.Streams.Add(stream);
                    }

                    foreach (PlexStreamResponse subtitleStream in streams.Filter(s => s.StreamType == 3))
                    {
                        var stream = new MediaStream
                        {
                            MediaVersionId = version.Id,
                            MediaStreamKind = MediaStreamKind.Subtitle,
                            Index = subtitleStream.Index,
                            Codec = subtitleStream.Codec,
                            Default = subtitleStream.Default,
                            Forced = subtitleStream.Forced,
                            Language = subtitleStream.LanguageCode
                        };

                        version.Streams.Add(stream);
                    }

                    return version;
                });
        }

        private PlexShow ProjectToShow(PlexMetadataResponse response, int mediaSourceId)
        {
            DateTime dateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime;
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new ShowMetadata
            {
                Title = response.Title,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(response.Title),
                Plot = response.Summary,
                Year = response.Year,
                Tagline = response.Tagline,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime,
                Genres = Optional(response.Genre).Flatten().Map(g => new Genre { Name = g.Tag }).ToList(),
                Tags = new List<Tag>(),
                Studios = new List<Studio>()
            };

            if (!string.IsNullOrWhiteSpace(response.Studio))
            {
                metadata.Studios.Add(new Studio { Name = response.Studio });
            }

            if (DateTime.TryParse(response.OriginallyAvailableAt, out DateTime releaseDate))
            {
                metadata.ReleaseDate = releaseDate;
            }

            if (!string.IsNullOrWhiteSpace(response.Thumb))
            {
                var path = $"plex/{mediaSourceId}{response.Thumb}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            if (!string.IsNullOrWhiteSpace(response.Art))
            {
                var path = $"plex/{mediaSourceId}{response.Art}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            var show = new PlexShow
            {
                Key = response.Key,
                ShowMetadata = new List<ShowMetadata> { metadata }
            };

            return show;
        }

        private PlexSeason ProjectToSeason(PlexMetadataResponse response, int mediaSourceId)
        {
            DateTime dateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime;
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new SeasonMetadata
            {
                Title = response.Title,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(response.Title),
                Year = response.Year,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime
            };

            if (!string.IsNullOrWhiteSpace(response.Thumb))
            {
                var path = $"plex/{mediaSourceId}{response.Thumb}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            if (!string.IsNullOrWhiteSpace(response.Art))
            {
                var path = $"plex/{mediaSourceId}{response.Art}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            var season = new PlexSeason
            {
                Key = response.Key,
                SeasonNumber = response.Index,
                SeasonMetadata = new List<SeasonMetadata> { metadata }
            };

            return season;
        }

        private PlexEpisode ProjectToEpisode(PlexMetadataResponse response, int mediaSourceId)
        {
            PlexMediaResponse media = response.Media.Head();
            PlexPartResponse part = media.Part.Head();
            DateTime dateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime;
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new EpisodeMetadata
            {
                Title = response.Title,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(response.Title),
                Plot = response.Summary,
                Year = response.Year,
                Tagline = response.Tagline,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime
            };

            if (DateTime.TryParse(response.OriginallyAvailableAt, out DateTime releaseDate))
            {
                metadata.ReleaseDate = releaseDate;
            }

            if (!string.IsNullOrWhiteSpace(response.Thumb))
            {
                var path = $"plex/{mediaSourceId}{response.Thumb}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.Thumbnail,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            var version = new MediaVersion
            {
                Name = "Main",
                Duration = TimeSpan.FromMilliseconds(media.Duration),
                Width = media.Width,
                Height = media.Height,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime,
                MediaFiles = new List<MediaFile>
                {
                    new PlexMediaFile
                    {
                        PlexId = part.Id,
                        Key = part.Key,
                        Path = part.File
                    }
                },
                // specifically omit stream details
                Streams = new List<MediaStream>()
            };

            var episode = new PlexEpisode
            {
                Key = response.Key,
                EpisodeNumber = response.Index,
                EpisodeMetadata = new List<EpisodeMetadata> { metadata },
                MediaVersions = new List<MediaVersion> { version }
            };

            return episode;
        }
    }
}
