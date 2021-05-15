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

                var version = new MediaVersion
                {
                    Name = "Main",
                    Duration = TimeSpan.FromTicks(item.RunTimeTicks),
                    DateAdded = item.DateCreated.UtcDateTime,
                    MediaFiles = new List<MediaFile>
                    {
                        new()
                        {
                            Path = item.Path
                        }
                    },
                    Streams = new List<MediaStream>()
                };

                MovieMetadata metadata = ProjectToMovieMetadata(item, mediaSourceId);

                var movie = new JellyfinMovie
                {
                    ItemId = item.Id,
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
                Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList(),
                Actors = Optional(item.People).Flatten().Map(r => ProjectToModel(r, dateAdded)).ToList(),
                Artwork = new List<Artwork>()
            };
            
            // set order on actors
            for (int i = 0; i < metadata.Actors.Count; i++)
            {
                metadata.Actors[i].Order = i;
            }

            if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
            {
                metadata.ReleaseDate = releaseDate;
            }

            if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
            {
                var poster = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = $"jellyfin:///Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(poster);
            }

            if (item.BackdropImageTags.Any())
            {
                var fanArt = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = $"jellyfin:///Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(fanArt);
            }

            return metadata;
        }
        
        private Actor ProjectToModel(JellyfinPersonResponse person, DateTime dateAdded)
        {
            var actor = new Actor { Name = person.Name, Role = person.Role };
            if (!string.IsNullOrWhiteSpace(person.Id) && !string.IsNullOrWhiteSpace(person.PrimaryImageTag))
            {
                actor.Artwork = new Artwork
                {
                    Path = $"jellyfin:///Items/{person.Id}/Images/Primary?tag={person.PrimaryImageTag}",
                    ArtworkKind = ArtworkKind.Thumbnail,
                    DateAdded = dateAdded
                };
            }

            return actor;
        }
    }
}
