using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    public class JellyfinApiClient : IJellyfinApiClient
    {
        private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
        private readonly ILogger<JellyfinApiClient> _logger;
        private readonly IMemoryCache _memoryCache;

        public JellyfinApiClient(
            IMemoryCache memoryCache,
            IFallbackMetadataProvider fallbackMetadataProvider,
            ILogger<JellyfinApiClient> logger)
        {
            _memoryCache = memoryCache;
            _fallbackMetadataProvider = fallbackMetadataProvider;
            _logger = logger;
        }

        public async Task<Either<BaseError, string>> GetServerName(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                JellyfinConfigurationResponse config = await service.GetConfiguration(apiKey);
                return config.ServerName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin server name");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                List<JellyfinLibraryResponse> libraries = await service.GetLibraries(apiKey);
                return libraries
                    .Map(Project)
                    .Somes()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin libraries");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                List<JellyfinUserResponse> users = await service.GetUsers(apiKey);
                Option<string> maybeUserId = users
                    .Filter(user => user.Policy.IsAdministrator)
                    .Map(user => user.Id)
                    .HeadOrNone();

                return maybeUserId.ToEither(BaseError.New("Unable to locate jellyfin admin user"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin admin user id");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<JellyfinMovie>>> GetMovieLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId)
        {
            try
            {
                if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{mediaSourceId}", out string userId))
                {
                    IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                    JellyfinLibraryItemsResponse items = await service.GetLibraryItems(apiKey, userId, libraryId);
                    return items.Items
                        .Map(i => ProjectToMovie(i, mediaSourceId))
                        .Somes()
                        .ToList();
                }

                return BaseError.New("Jellyfin admin user id is not available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin library items");
                return BaseError.New(ex.Message);
            }
        }

        private static Option<JellyfinLibrary> Project(JellyfinLibraryResponse response) =>
            response.CollectionType.ToLowerInvariant() switch
            {
                "tvshows" => new JellyfinLibrary
                {
                    ItemId = response.ItemId,
                    Name = response.Name,
                    MediaKind = LibraryMediaKind.Shows,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } }
                },
                "movies" => new JellyfinLibrary
                {
                    ItemId = response.ItemId,
                    Name = response.Name,
                    MediaKind = LibraryMediaKind.Movies,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } }
                },
                // TODO: ??? for music libraries
                _ => None
            };

        private Option<JellyfinMovie> ProjectToMovie(JellyfinLibraryItemResponse item, int mediaSourceId)
        {
            try
            {
                if (item.LocationType != "FileSystem")
                {
                    return None;
                }

                Option<JellyfinMediaStreamResponse> maybeVideoStream = item.MediaStreams.Find(s => s.Type == "Video");
                if (maybeVideoStream.IsNone)
                {
                    return None;
                }

                JellyfinMediaStreamResponse videoStreamResponse = maybeVideoStream.ValueUnsafe();
                var videoStream = new MediaStream
                {
                    MediaStreamKind = MediaStreamKind.Video,
                    Codec = videoStreamResponse.Codec,
                    Index = videoStreamResponse.Index,
                    Language = videoStreamResponse.Language,
                    Default = videoStreamResponse.IsDefault,
                    Forced = videoStreamResponse.IsForced,
                    Profile = videoStreamResponse.Profile
                };

                var version = new MediaVersion
                {
                    Name = "Main",
                    Duration = TimeSpan.FromTicks(item.RunTimeTicks),
                    Height = videoStreamResponse.Height.Value,
                    Width = videoStreamResponse.Width.Value,
                    DateAdded = item.DateCreated.UtcDateTime,
                    VideoScanKind = videoStreamResponse.IsInterlaced == true
                        ? VideoScanKind.Interlaced
                        : VideoScanKind.Progressive,
                    SampleAspectRatio = videoStreamResponse.AspectRatio,
                    MediaFiles = new List<MediaFile>
                    {
                        new()
                        {
                            Path = item.Path
                        }
                    },
                    Streams = new List<MediaStream>
                    {
                        videoStream
                    }
                };

                videoStream.MediaVersion = version;

                MovieMetadata metadata = ProjectToMovieMetadata(item, mediaSourceId);

                var movie = new JellyfinMovie
                {
                    Etag = item.Etag,
                    MediaVersions = new List<MediaVersion> { version },
                    MovieMetadata = new List<MovieMetadata> { metadata }
                };

                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error projecting Jellyfin movie");
                return None;
            }
        }

        private MovieMetadata ProjectToMovieMetadata(JellyfinLibraryItemResponse item, int mediaSourceId)
        {
            DateTime dateAdded = item.DateCreated.UtcDateTime;
            // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;

            var metadata = new MovieMetadata
            {
                Title = item.Name,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
                Plot = item.Overview,
                Year = item.ProductionYear,
                Tagline = Optional(item.Taglines).Flatten().HeadOrNone().IfNone(string.Empty),
                DateAdded = dateAdded,
                Genres = Optional(item.Genres).Flatten().Map(g => new Genre { Name = g }).ToList(),
                Tags = Optional(item.Tags).Flatten().Map(t => new Tag { Name = t }).ToList(),
                Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList()
                // Actors = Optional(item.Role).Flatten().Map(r => ProjectToModel(r, dateAdded, lastWriteTime))
                //     .ToList()
            };

            // if (!string.IsNullOrWhiteSpace(item.Studio))
            // {
            //     metadata.Studios.Add(new Studio { Name = item.Studio });
            // }

            if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
            {
                metadata.ReleaseDate = releaseDate;
            }

            // if (!string.IsNullOrWhiteSpace(item.Thumb))
            // {
            //     var path = $"plex/{mediaSourceId}{item.Thumb}";
            //     var artwork = new Artwork
            //     {
            //         ArtworkKind = ArtworkKind.Poster,
            //         Path = path,
            //         DateAdded = dateAdded,
            //         DateUpdated = lastWriteTime
            //     };
            //
            //     metadata.Artwork ??= new List<Artwork>();
            //     metadata.Artwork.Add(artwork);
            // }
            //
            // if (!string.IsNullOrWhiteSpace(item.Art))
            // {
            //     var path = $"plex/{mediaSourceId}{item.Art}";
            //     var artwork = new Artwork
            //     {
            //         ArtworkKind = ArtworkKind.FanArt,
            //         Path = path,
            //         DateAdded = dateAdded,
            //         DateUpdated = lastWriteTime
            //     };
            //
            //     metadata.Artwork ??= new List<Artwork>();
            //     metadata.Artwork.Add(artwork);
            // }

            return metadata;
        }
    }
}
