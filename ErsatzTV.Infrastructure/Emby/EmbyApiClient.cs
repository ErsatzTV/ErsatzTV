using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Emby;
using ErsatzTV.Infrastructure.Emby.Models;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Emby
{
    public class EmbyApiClient : IEmbyApiClient
    {
        private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
        private readonly ILogger<EmbyApiClient> _logger;
        private readonly IMemoryCache _memoryCache;

        public EmbyApiClient(
            IMemoryCache memoryCache,
            IFallbackMetadataProvider fallbackMetadataProvider,
            ILogger<EmbyApiClient> logger)
        {
            _memoryCache = memoryCache;
            _fallbackMetadataProvider = fallbackMetadataProvider;
            _logger = logger;
        }

        public async Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(
            string address,
            string apiKey)
        {
            try
            {
                IEmbyApi service = RestService.For<IEmbyApi>(address);
                return await service.GetSystemInformation(apiKey)
                    .Map(response => new EmbyServerInformation(response.ServerName, response.OperatingSystem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emby server name");
                return BaseError.New(ex.Message);
            }
        }

        // public async Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey)
        // {
        //     try
        //     {
        //         IEmbyApi service = RestService.For<IEmbyApi>(address);
        //         List<EmbyLibraryResponse> libraries = await service.GetLibraries(apiKey);
        //         return libraries
        //             .Map(Project)
        //             .Somes()
        //             .ToList();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby libraries");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // public async Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey)
        // {
        //     try
        //     {
        //         IEmbyApi service = RestService.For<IEmbyApi>(address);
        //         List<EmbyUserResponse> users = await service.GetUsers(apiKey);
        //         Option<string> maybeUserId = users
        //             .Filter(user => user.Policy.IsAdministrator)
        //             .Map(user => user.Id)
        //             .HeadOrNone();
        //
        //         return maybeUserId.ToEither(BaseError.New("Unable to locate emby admin user"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby admin user id");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // public async Task<Either<BaseError, List<EmbyMovie>>> GetMovieLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string libraryId)
        // {
        //     try
        //     {
        //         if (_memoryCache.TryGetValue($"emby_admin_user_id.{mediaSourceId}", out string userId))
        //         {
        //             IEmbyApi service = RestService.For<IEmbyApi>(address);
        //             EmbyLibraryItemsResponse items = await service.GetMovieLibraryItems(apiKey, userId, libraryId);
        //             return items.Items
        //                 .Map(ProjectToMovie)
        //                 .Somes()
        //                 .ToList();
        //         }
        //
        //         return BaseError.New("Emby admin user id is not available");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby movie library items");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // public async Task<Either<BaseError, List<EmbyShow>>> GetShowLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string libraryId)
        // {
        //     try
        //     {
        //         if (_memoryCache.TryGetValue($"emby_admin_user_id.{mediaSourceId}", out string userId))
        //         {
        //             IEmbyApi service = RestService.For<IEmbyApi>(address);
        //             EmbyLibraryItemsResponse items = await service.GetShowLibraryItems(apiKey, userId, libraryId);
        //             return items.Items
        //                 .Map(ProjectToShow)
        //                 .Somes()
        //                 .ToList();
        //         }
        //
        //         return BaseError.New("Emby admin user id is not available");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby show library items");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // public async Task<Either<BaseError, List<EmbySeason>>> GetSeasonLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string showId)
        // {
        //     try
        //     {
        //         if (_memoryCache.TryGetValue($"emby_admin_user_id.{mediaSourceId}", out string userId))
        //         {
        //             IEmbyApi service = RestService.For<IEmbyApi>(address);
        //             EmbyLibraryItemsResponse items = await service.GetSeasonLibraryItems(apiKey, userId, showId);
        //             return items.Items
        //                 .Map(ProjectToSeason)
        //                 .Somes()
        //                 .ToList();
        //         }
        //
        //         return BaseError.New("Emby admin user id is not available");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby show library items");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // public async Task<Either<BaseError, List<EmbyEpisode>>> GetEpisodeLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string seasonId)
        // {
        //     try
        //     {
        //         if (_memoryCache.TryGetValue($"emby_admin_user_id.{mediaSourceId}", out string userId))
        //         {
        //             IEmbyApi service = RestService.For<IEmbyApi>(address);
        //             EmbyLibraryItemsResponse items = await service.GetEpisodeLibraryItems(apiKey, userId, seasonId);
        //             return items.Items
        //                 .Map(ProjectToEpisode)
        //                 .Somes()
        //                 .ToList();
        //         }
        //
        //         return BaseError.New("Emby admin user id is not available");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting emby episode library items");
        //         return BaseError.New(ex.Message);
        //     }
        // }
        //
        // private static Option<EmbyLibrary> Project(EmbyLibraryResponse response) =>
        //     response.CollectionType?.ToLowerInvariant() switch
        //     {
        //         "tvshows" => new EmbyLibrary
        //         {
        //             ItemId = response.ItemId,
        //             Name = response.Name,
        //             MediaKind = LibraryMediaKind.Shows,
        //             ShouldSyncItems = false,
        //             Paths = new List<LibraryPath> { new() { Path = $"emby://{response.ItemId}" } }
        //         },
        //         "movies" => new EmbyLibrary
        //         {
        //             ItemId = response.ItemId,
        //             Name = response.Name,
        //             MediaKind = LibraryMediaKind.Movies,
        //             ShouldSyncItems = false,
        //             Paths = new List<LibraryPath> { new() { Path = $"emby://{response.ItemId}" } }
        //         },
        //         // TODO: ??? for music libraries
        //         _ => None
        //     };
        //
        // private Option<EmbyMovie> ProjectToMovie(EmbyLibraryItemResponse item)
        // {
        //     try
        //     {
        //         if (item.LocationType != "FileSystem")
        //         {
        //             return None;
        //         }
        //
        //         var version = new MediaVersion
        //         {
        //             Name = "Main",
        //             Duration = TimeSpan.FromTicks(item.RunTimeTicks),
        //             DateAdded = item.DateCreated.UtcDateTime,
        //             MediaFiles = new List<MediaFile>
        //             {
        //                 new()
        //                 {
        //                     Path = item.Path
        //                 }
        //             },
        //             Streams = new List<MediaStream>()
        //         };
        //
        //         MovieMetadata metadata = ProjectToMovieMetadata(item);
        //
        //         var movie = new EmbyMovie
        //         {
        //             ItemId = item.Id,
        //             Etag = item.Etag,
        //             MediaVersions = new List<MediaVersion> { version },
        //             MovieMetadata = new List<MovieMetadata> { metadata }
        //         };
        //
        //         return movie;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Error projecting Emby movie");
        //         return None;
        //     }
        // }
        //
        // private MovieMetadata ProjectToMovieMetadata(EmbyLibraryItemResponse item)
        // {
        //     DateTime dateAdded = item.DateCreated.UtcDateTime;
        //     // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;
        //
        //     var metadata = new MovieMetadata
        //     {
        //         Title = item.Name,
        //         SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
        //         Plot = item.Overview,
        //         Year = item.ProductionYear,
        //         Tagline = Optional(item.Taglines).Flatten().HeadOrNone().IfNone(string.Empty),
        //         DateAdded = dateAdded,
        //         Genres = Optional(item.Genres).Flatten().Map(g => new Genre { Name = g }).ToList(),
        //         Tags = Optional(item.Tags).Flatten().Map(t => new Tag { Name = t }).ToList(),
        //         Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList(),
        //         Actors = Optional(item.People).Flatten().Map(r => ProjectToModel(r, dateAdded)).ToList(),
        //         Artwork = new List<Artwork>()
        //     };
        //
        //     // set order on actors
        //     for (var i = 0; i < metadata.Actors.Count; i++)
        //     {
        //         metadata.Actors[i].Order = i;
        //     }
        //
        //     if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
        //     {
        //         metadata.ReleaseDate = releaseDate;
        //     }
        //
        //     if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
        //     {
        //         var poster = new Artwork
        //         {
        //             ArtworkKind = ArtworkKind.Poster,
        //             Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
        //             DateAdded = dateAdded
        //         };
        //         metadata.Artwork.Add(poster);
        //     }
        //
        //     if (item.BackdropImageTags.Any())
        //     {
        //         var fanArt = new Artwork
        //         {
        //             ArtworkKind = ArtworkKind.FanArt,
        //             Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
        //             DateAdded = dateAdded
        //         };
        //         metadata.Artwork.Add(fanArt);
        //     }
        //
        //     return metadata;
        // }
        //
        // private Actor ProjectToModel(EmbyPersonResponse person, DateTime dateAdded)
        // {
        //     var actor = new Actor { Name = person.Name, Role = person.Role };
        //     if (!string.IsNullOrWhiteSpace(person.Id) && !string.IsNullOrWhiteSpace(person.PrimaryImageTag))
        //     {
        //         actor.Artwork = new Artwork
        //         {
        //             Path = $"emby://Items/{person.Id}/Images/Primary?tag={person.PrimaryImageTag}",
        //             ArtworkKind = ArtworkKind.Thumbnail,
        //             DateAdded = dateAdded
        //         };
        //     }
        //
        //     return actor;
        // }
        //
        // private Option<EmbyShow> ProjectToShow(EmbyLibraryItemResponse item)
        // {
        //     try
        //     {
        //         if (item.LocationType != "FileSystem")
        //         {
        //             return None;
        //         }
        //
        //         ShowMetadata metadata = ProjectToShowMetadata(item);
        //
        //         var show = new EmbyShow
        //         {
        //             ItemId = item.Id,
        //             Etag = item.Etag,
        //             ShowMetadata = new List<ShowMetadata> { metadata }
        //         };
        //
        //         return show;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Error projecting Emby show");
        //         return None;
        //     }
        // }
        //
        // private ShowMetadata ProjectToShowMetadata(EmbyLibraryItemResponse item)
        // {
        //     DateTime dateAdded = item.DateCreated.UtcDateTime;
        //     // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;
        //
        //     var metadata = new ShowMetadata
        //     {
        //         Title = item.Name,
        //         SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
        //         Plot = item.Overview,
        //         Year = item.ProductionYear,
        //         Tagline = Optional(item.Taglines).Flatten().HeadOrNone().IfNone(string.Empty),
        //         DateAdded = dateAdded,
        //         Genres = Optional(item.Genres).Flatten().Map(g => new Genre { Name = g }).ToList(),
        //         Tags = Optional(item.Tags).Flatten().Map(t => new Tag { Name = t }).ToList(),
        //         Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList(),
        //         Actors = Optional(item.People).Flatten().Map(r => ProjectToModel(r, dateAdded)).ToList(),
        //         Artwork = new List<Artwork>()
        //     };
        //
        //     // set order on actors
        //     for (var i = 0; i < metadata.Actors.Count; i++)
        //     {
        //         metadata.Actors[i].Order = i;
        //     }
        //
        //     if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
        //     {
        //         metadata.ReleaseDate = releaseDate;
        //     }
        //
        //     if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
        //     {
        //         var poster = new Artwork
        //         {
        //             ArtworkKind = ArtworkKind.Poster,
        //             Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
        //             DateAdded = dateAdded
        //         };
        //         metadata.Artwork.Add(poster);
        //     }
        //
        //     if (item.BackdropImageTags.Any())
        //     {
        //         var fanArt = new Artwork
        //         {
        //             ArtworkKind = ArtworkKind.FanArt,
        //             Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
        //             DateAdded = dateAdded
        //         };
        //         metadata.Artwork.Add(fanArt);
        //     }
        //
        //     return metadata;
        // }
        //
        // private Option<EmbySeason> ProjectToSeason(EmbyLibraryItemResponse item)
        // {
        //     try
        //     {
        //         if (item.LocationType != "FileSystem")
        //         {
        //             return None;
        //         }
        //
        //         DateTime dateAdded = item.DateCreated.UtcDateTime;
        //         // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;
        //
        //         var metadata = new SeasonMetadata
        //         {
        //             Title = item.Name,
        //             SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
        //             Year = item.ProductionYear,
        //             DateAdded = dateAdded,
        //             Artwork = new List<Artwork>()
        //         };
        //
        //         if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
        //         {
        //             var poster = new Artwork
        //             {
        //                 ArtworkKind = ArtworkKind.Poster,
        //                 Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
        //                 DateAdded = dateAdded
        //             };
        //             metadata.Artwork.Add(poster);
        //         }
        //
        //         if (item.BackdropImageTags.Any())
        //         {
        //             var fanArt = new Artwork
        //             {
        //                 ArtworkKind = ArtworkKind.FanArt,
        //                 Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
        //                 DateAdded = dateAdded
        //             };
        //             metadata.Artwork.Add(fanArt);
        //         }
        //
        //         var season = new EmbySeason
        //         {
        //             ItemId = item.Id,
        //             Etag = item.Etag,
        //             SeasonMetadata = new List<SeasonMetadata> { metadata }
        //         };
        //
        //         if (item.IndexNumber.HasValue)
        //         {
        //             season.SeasonNumber = item.IndexNumber.Value;
        //         }
        //
        //         return season;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Error projecting Emby show");
        //         return None;
        //     }
        // }
        //
        // private Option<EmbyEpisode> ProjectToEpisode(EmbyLibraryItemResponse item)
        // {
        //     try
        //     {
        //         if (item.LocationType != "FileSystem")
        //         {
        //             return None;
        //         }
        //
        //         var version = new MediaVersion
        //         {
        //             Name = "Main",
        //             Duration = TimeSpan.FromTicks(item.RunTimeTicks),
        //             DateAdded = item.DateCreated.UtcDateTime,
        //             MediaFiles = new List<MediaFile>
        //             {
        //                 new()
        //                 {
        //                     Path = item.Path
        //                 }
        //             },
        //             Streams = new List<MediaStream>()
        //         };
        //
        //         EpisodeMetadata metadata = ProjectToEpisodeMetadata(item);
        //
        //         var episode = new EmbyEpisode
        //         {
        //             ItemId = item.Id,
        //             Etag = item.Etag,
        //             MediaVersions = new List<MediaVersion> { version },
        //             EpisodeMetadata = new List<EpisodeMetadata> { metadata }
        //         };
        //
        //         if (item.IndexNumber.HasValue)
        //         {
        //             episode.EpisodeNumber = item.IndexNumber.Value;
        //         }
        //
        //         return episode;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Error projecting Emby movie");
        //         return None;
        //     }
        // }
        //
        // private EpisodeMetadata ProjectToEpisodeMetadata(EmbyLibraryItemResponse item)
        // {
        //     DateTime dateAdded = item.DateCreated.UtcDateTime;
        //     // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;
        //
        //     var metadata = new EpisodeMetadata
        //     {
        //         Title = item.Name,
        //         SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
        //         Plot = item.Overview,
        //         Year = item.ProductionYear,
        //         DateAdded = dateAdded,
        //         Genres = new List<Genre>(),
        //         Tags = new List<Tag>(),
        //         Studios = new List<Studio>(),
        //         Actors = new List<Actor>(),
        //         Artwork = new List<Artwork>()
        //     };
        //
        //     if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
        //     {
        //         metadata.ReleaseDate = releaseDate;
        //     }
        //
        //     if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
        //     {
        //         var thumbnail = new Artwork
        //         {
        //             ArtworkKind = ArtworkKind.Thumbnail,
        //             Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
        //             DateAdded = dateAdded
        //         };
        //         metadata.Artwork.Add(thumbnail);
        //     }
        //
        //     return metadata;
        // }
    }
}
