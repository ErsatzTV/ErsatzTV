using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin;

public class JellyfinApiClient : IJellyfinApiClient
{
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly ILogger<JellyfinApiClient> _logger;
    private readonly IMemoryCache _memoryCache;

    public JellyfinApiClient(
        IMemoryCache memoryCache,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        ILogger<JellyfinApiClient> logger)
    {
        _memoryCache = memoryCache;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _logger = logger;
    }

    public async Task<Either<BaseError, JellyfinServerInformation>> GetServerInformation(
        string address,
        string apiKey)
    {
        try
        {
            IJellyfinApi service = RestService.For<IJellyfinApi>(address);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            return await service.GetSystemInformation(apiKey, cts.Token)
                .Map(response => new JellyfinServerInformation(response.ServerName, response.OperatingSystem));
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout getting jellyfin server name");
            return BaseError.New("Jellyfin did not respond in time");
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

    public IAsyncEnumerable<JellyfinMovie> GetMovieLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library) =>
        GetPagedLibraryItems(
            address,
            apiKey,
            library,
            library.MediaSourceId,
            library.ItemId,
            JellyfinItemType.Movie,
            (service, userId, itemId, skip, pageSize) => service.GetMovieLibraryItems(
                apiKey,
                userId,
                itemId,
                startIndex: skip,
                limit: pageSize),
            (maybeLibrary, item) => maybeLibrary.Map(lib => ProjectToMovie(lib, item)).Flatten());

    public IAsyncEnumerable<JellyfinShow> GetShowLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library) =>
        GetPagedLibraryItems(
            address,
            apiKey,
            library,
            library.MediaSourceId,
            library.ItemId,
            JellyfinItemType.Show,
            (service, userId, itemId, skip, pageSize) => service.GetShowLibraryItems(
                apiKey,
                userId,
                itemId,
                startIndex: skip,
                limit: pageSize),
            (_, item) => ProjectToShow(item));

    public IAsyncEnumerable<JellyfinSeason> GetSeasonLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string showId) =>
        GetPagedLibraryItems(
            address,
            apiKey,
            library,
            library.MediaSourceId,
            showId,
            JellyfinItemType.Season,
            (service, userId, _, skip, pageSize) => service.GetSeasonLibraryItems(
                apiKey,
                userId,
                showId,
                startIndex: skip,
                limit: pageSize),
            (_, item) => ProjectToSeason(item));

    public IAsyncEnumerable<JellyfinEpisode> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string seasonId) =>
        GetPagedLibraryItems(
            address,
            apiKey,
            library,
            library.MediaSourceId,
            seasonId,
            JellyfinItemType.Episode,
            (service, userId, _, skip, pageSize) => service.GetEpisodeLibraryItems(
                apiKey,
                userId,
                seasonId,
                startIndex: skip,
                limit: pageSize),
            (maybeLibrary, item) => maybeLibrary.Map(lib => ProjectToEpisode(lib, item)).Flatten());

    public IAsyncEnumerable<JellyfinCollection> GetCollectionLibraryItems(
        string address,
        string apiKey,
        int mediaSourceId)
    {
        // TODO: should we enumerate collection libraries here?

        if (_memoryCache.TryGetValue("jellyfin_collections_library_item_id", out string itemId))
        {
            return GetPagedLibraryItems(
                address,
                apiKey,
                None,
                mediaSourceId,
                itemId,
                JellyfinItemType.Collection,
                (service, userId, _, skip, pageSize) => service.GetCollectionLibraryItems(
                    apiKey,
                    userId,
                    itemId,
                    startIndex: skip,
                    limit: pageSize),
                (_, item) => ProjectToCollection(item));
        }

        return AsyncEnumerable.Empty<JellyfinCollection>();
    }

    public async Task<Either<BaseError, List<MediaItem>>> GetCollectionItems(
        string address,
        string apiKey,
        int mediaSourceId,
        string collectionId)
    {
        try
        {
            if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{mediaSourceId}", out string userId))
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                JellyfinLibraryItemsResponse items = await service.GetCollectionItems(
                    apiKey,
                    userId,
                    collectionId);
                return items.Items
                    .Map(ProjectToCollectionMediaItem)
                    .Somes()
                    .ToList();
            }

            return BaseError.New("Jellyfin admin user id is not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jellyfin collection items");
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, int>> GetLibraryItemCount(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string parentId,
        string includeItemTypes,
        bool excludeFolders)
    {
        try
        {
            if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{library.MediaSourceId}", out string userId))
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                JellyfinLibraryItemsResponse items = await service.GetLibraryStats(
                    apiKey,
                    userId,
                    parentId,
                    includeItemTypes,
                    filters: excludeFolders ? "IsNotFolder" : null);
                return items.TotalRecordCount;
            }

            return BaseError.New("Jellyfin admin user id is not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jellyfin library item count");
            return BaseError.New(ex.Message);
        }
    }

    private async IAsyncEnumerable<TItem> GetPagedLibraryItems<TItem>(
        string address,
        string apiKey,
        Option<JellyfinLibrary> maybeLibrary,
        int mediaSourceId,
        string parentId,
        string itemType,
        Func<IJellyfinApi, string, string, int, int, Task<JellyfinLibraryItemsResponse>> getItems,
        Func<Option<JellyfinLibrary>, JellyfinLibraryItemResponse, Option<TItem>> mapper)
    {
        if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{mediaSourceId}", out string userId))
        {
            IJellyfinApi service = RestService.For<IJellyfinApi>(address);
            int size = await service
                .GetLibraryStats(apiKey, userId, parentId, itemType)
                .Map(r => r.TotalRecordCount);

            const int PAGE_SIZE = 10;

            int pages = (size - 1) / PAGE_SIZE + 1;

            for (var i = 0; i < pages; i++)
            {
                int skip = i * PAGE_SIZE;

                Task<IEnumerable<TItem>> result = getItems(service, userId, parentId, skip, PAGE_SIZE)
                    .Map(items => items.Items.Map(item => mapper(maybeLibrary, item)).Somes());

                foreach (TItem item in await result)
                {
                    yield return item;
                }
            }
        }
    }

    private Option<MediaItem> ProjectToCollectionMediaItem(JellyfinLibraryItemResponse item)
    {
        try
        {
            if (item.LocationType != "FileSystem")
            {
                return None;
            }

            if (Path.GetExtension(item.Path)?.ToLowerInvariant() == ".strm")
            {
                _logger.LogWarning("STRM files are not supported; skipping {Path}", item.Path);
                return None;
            }

            return item.Type switch
            {
                "Movie" => new JellyfinMovie { ItemId = item.Id },
                "Series" => new JellyfinShow { ItemId = item.Id },
                "Season" => new JellyfinSeason { ItemId = item.Id },
                "Episode" => new JellyfinEpisode { ItemId = item.Id },
                _ => None
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin collection media item");
            return None;
        }
    }

    private Option<JellyfinLibrary> Project(JellyfinLibraryResponse response) =>
        response.CollectionType?.ToLowerInvariant() switch
        {
            "tvshows" => new JellyfinLibrary
            {
                ItemId = response.ItemId,
                Name = response.Name,
                MediaKind = LibraryMediaKind.Shows,
                ShouldSyncItems = false,
                Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } },
                PathInfos = GetPathInfos(response)
            },
            "movies" => new JellyfinLibrary
            {
                ItemId = response.ItemId,
                Name = response.Name,
                MediaKind = LibraryMediaKind.Movies,
                ShouldSyncItems = false,
                Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } },
                PathInfos = GetPathInfos(response)
            },
            // TODO: ??? for music libraries
            "boxsets" => CacheCollectionLibraryId(response.ItemId),
            _ => None
        };

    private List<JellyfinPathInfo> GetPathInfos(JellyfinLibraryResponse response) =>
        response.LibraryOptions.PathInfos
            .Filter(pi => !string.IsNullOrWhiteSpace(pi.NetworkPath))
            .Map(
                pi => new JellyfinPathInfo
                {
                    Path = pi.Path,
                    NetworkPath = pi.NetworkPath
                }).ToList();

    private Option<JellyfinLibrary> CacheCollectionLibraryId(string itemId)
    {
        _memoryCache.Set("jellyfin_collections_library_item_id", itemId);
        return None;
    }

    private Option<JellyfinMovie> ProjectToMovie(JellyfinLibrary library, JellyfinLibraryItemResponse item)
    {
        try
        {
            if (item.LocationType != "FileSystem")
            {
                return None;
            }

            if (Path.GetExtension(item.Path)?.ToLowerInvariant() == ".strm")
            {
                _logger.LogInformation("STRM files are not supported; skipping {Path}", item.Path);
                return None;
            }

            string path = item.Path ?? string.Empty;
            foreach (JellyfinPathInfo pathInfo in library.PathInfos.Filter(
                         pi => !string.IsNullOrWhiteSpace(pi.NetworkPath)))
            {
                if (path.StartsWith(pathInfo.NetworkPath, StringComparison.Ordinal))
                {
                    path = _jellyfinPathReplacementService.ReplaceNetworkPath(
                        (JellyfinMediaSource)library.MediaSource,
                        path,
                        pathInfo.NetworkPath,
                        pathInfo.Path);
                }
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
                        Path = path
                    }
                },
                Streams = new List<MediaStream>()
            };

            MovieMetadata metadata = ProjectToMovieMetadata(item);

            var movie = new JellyfinMovie
            {
                ItemId = item.Id,
                Etag = item.Etag,
                MediaVersions = new List<MediaVersion> { version },
                MovieMetadata = new List<MovieMetadata> { metadata },
                TraktListItems = new List<TraktListItem>()
            };

            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin movie");
            return None;
        }
    }

    private MovieMetadata ProjectToMovieMetadata(JellyfinLibraryItemResponse item)
    {
        DateTime dateAdded = item.DateCreated.UtcDateTime;
        // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;

        var metadata = new MovieMetadata
        {
            MetadataKind = MetadataKind.External,
            Title = item.Name,
            SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
            Plot = item.Overview,
            Year = item.ProductionYear,
            Tagline = Optional(item.Taglines).Flatten().HeadOrNone().IfNone(string.Empty),
            DateAdded = dateAdded,
            ContentRating = item.OfficialRating,
            Genres = Optional(item.Genres).Flatten().Map(g => new Genre { Name = g }).ToList(),
            Tags = Optional(item.Tags).Flatten().Map(t => new Tag { Name = t }).ToList(),
            Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList(),
            Actors = Optional(item.People).Flatten().Collect(r => ProjectToActor(r, dateAdded)).ToList(),
            Directors = Optional(item.People).Flatten().Collect(r => ProjectToDirector(r)).ToList(),
            Writers = Optional(item.People).Flatten().Collect(r => ProjectToWriter(r)).ToList(),
            Artwork = new List<Artwork>(),
            Guids = GuidsFromProviderIds(item.ProviderIds)
        };

        // set order on actors
        for (var i = 0; i < metadata.Actors.Count; i++)
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
                Path = $"jellyfin://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(poster);
        }

        if (item.BackdropImageTags.Any())
        {
            var fanArt = new Artwork
            {
                ArtworkKind = ArtworkKind.FanArt,
                Path = $"jellyfin://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(fanArt);
        }

        return metadata;
    }

    private static Option<Actor> ProjectToActor(JellyfinPersonResponse person, DateTime dateAdded)
    {
        if (person.Type?.ToLowerInvariant() != "actor")
        {
            return None;
        }

        var actor = new Actor { Name = person.Name, Role = person.Role };
        if (!string.IsNullOrWhiteSpace(person.Id) && !string.IsNullOrWhiteSpace(person.PrimaryImageTag))
        {
            actor.Artwork = new Artwork
            {
                Path = $"jellyfin://Items/{person.Id}/Images/Primary?tag={person.PrimaryImageTag}",
                ArtworkKind = ArtworkKind.Thumbnail,
                DateAdded = dateAdded
            };
        }

        return actor;
    }

    private static Option<Director> ProjectToDirector(JellyfinPersonResponse person)
    {
        if (person.Type?.ToLowerInvariant() != "director")
        {
            return None;
        }

        return new Director { Name = person.Name };
    }

    private static Option<Writer> ProjectToWriter(JellyfinPersonResponse person)
    {
        if (person.Type?.ToLowerInvariant() != "writer")
        {
            return None;
        }

        return new Writer { Name = person.Name };
    }

    private Option<JellyfinShow> ProjectToShow(JellyfinLibraryItemResponse item)
    {
        try
        {
            ShowMetadata metadata = ProjectToShowMetadata(item);

            var show = new JellyfinShow
            {
                ItemId = item.Id,
                Etag = item.Etag,
                ShowMetadata = new List<ShowMetadata> { metadata },
                TraktListItems = new List<TraktListItem>()
            };

            return show;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin show");
            return None;
        }
    }

    private ShowMetadata ProjectToShowMetadata(JellyfinLibraryItemResponse item)
    {
        DateTime dateAdded = item.DateCreated.UtcDateTime;
        // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;

        var metadata = new ShowMetadata
        {
            MetadataKind = MetadataKind.External,
            Title = item.Name,
            SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
            Plot = item.Overview,
            Year = item.ProductionYear,
            Tagline = Optional(item.Taglines).Flatten().HeadOrNone().IfNone(string.Empty),
            DateAdded = dateAdded,
            ContentRating = item.OfficialRating,
            Genres = Optional(item.Genres).Flatten().Map(g => new Genre { Name = g }).ToList(),
            Tags = Optional(item.Tags).Flatten().Map(t => new Tag { Name = t }).ToList(),
            Studios = Optional(item.Studios).Flatten().Map(s => new Studio { Name = s.Name }).ToList(),
            Actors = Optional(item.People).Flatten().Collect(r => ProjectToActor(r, dateAdded)).ToList(),
            Artwork = new List<Artwork>(),
            Guids = GuidsFromProviderIds(item.ProviderIds)
        };

        // set order on actors
        for (var i = 0; i < metadata.Actors.Count; i++)
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
                Path = $"jellyfin://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(poster);
        }

        if (!string.IsNullOrWhiteSpace(item.ImageTags.Thumb))
        {
            var thumb = new Artwork
            {
                ArtworkKind = ArtworkKind.Thumbnail,
                Path = $"jellyfin://Items/{item.Id}/Images/Thumb?tag={item.ImageTags.Thumb}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(thumb);
        }

        if (item.BackdropImageTags.Any())
        {
            var fanArt = new Artwork
            {
                ArtworkKind = ArtworkKind.FanArt,
                Path = $"jellyfin://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(fanArt);
        }

        return metadata;
    }

    private Option<JellyfinSeason> ProjectToSeason(JellyfinLibraryItemResponse item)
    {
        try
        {
            DateTime dateAdded = item.DateCreated.UtcDateTime;
            // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new SeasonMetadata
            {
                MetadataKind = MetadataKind.External,
                Title = item.Name,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
                Year = item.ProductionYear,
                DateAdded = dateAdded,
                Artwork = new List<Artwork>(),
                Guids = GuidsFromProviderIds(item.ProviderIds),
                Tags = new List<Tag>()
            };

            if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
            {
                var poster = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = $"jellyfin://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(poster);
            }

            if (item.BackdropImageTags.Any())
            {
                var fanArt = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = $"jellyfin://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(fanArt);
            }

            var season = new JellyfinSeason
            {
                ItemId = item.Id,
                Etag = item.Etag,
                SeasonMetadata = new List<SeasonMetadata> { metadata },
                TraktListItems = new List<TraktListItem>()
            };

            if (item.IndexNumber.HasValue)
            {
                season.SeasonNumber = item.IndexNumber.Value;
            }

            return season;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin season");
            return None;
        }
    }

    private Option<JellyfinCollection> ProjectToCollection(JellyfinLibraryItemResponse item)
    {
        try
        {
            return new JellyfinCollection
            {
                ItemId = item.Id,
                Etag = item.Etag,
                Name = item.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin collection");
            return None;
        }
    }

    private Option<JellyfinEpisode> ProjectToEpisode(JellyfinLibrary library, JellyfinLibraryItemResponse item)
    {
        try
        {
            if (item.LocationType != "FileSystem")
            {
                return None;
            }

            if (Path.GetExtension(item.Path)?.ToLowerInvariant() == ".strm")
            {
                _logger.LogWarning("STRM files are not supported; skipping {Path}", item.Path);
                return None;
            }

            string path = item.Path ?? string.Empty;
            foreach (JellyfinPathInfo pathInfo in library.PathInfos.Filter(
                         pi => !string.IsNullOrWhiteSpace(pi.NetworkPath)))
            {
                if (path.StartsWith(pathInfo.NetworkPath, StringComparison.Ordinal))
                {
                    path = _jellyfinPathReplacementService.ReplaceNetworkPath(
                        (JellyfinMediaSource)library.MediaSource,
                        path,
                        pathInfo.NetworkPath,
                        pathInfo.Path);
                }
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
                        Path = path
                    }
                },
                Streams = new List<MediaStream>()
            };

            EpisodeMetadata metadata = ProjectToEpisodeMetadata(item);

            var episode = new JellyfinEpisode
            {
                ItemId = item.Id,
                Etag = item.Etag,
                MediaVersions = new List<MediaVersion> { version },
                EpisodeMetadata = new List<EpisodeMetadata> { metadata },
                TraktListItems = new List<TraktListItem>()
            };

            return episode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Jellyfin episode");
            return None;
        }
    }

    private EpisodeMetadata ProjectToEpisodeMetadata(JellyfinLibraryItemResponse item)
    {
        DateTime dateAdded = item.DateCreated.UtcDateTime;
        // DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).DateTime;

        var metadata = new EpisodeMetadata
        {
            MetadataKind = MetadataKind.External,
            Title = item.Name,
            SortTitle = _fallbackMetadataProvider.GetSortTitle(item.Name),
            Plot = item.Overview,
            Year = item.ProductionYear,
            DateAdded = dateAdded,
            Genres = new List<Genre>(),
            Tags = new List<Tag>(),
            Studios = new List<Studio>(),
            Actors = new List<Actor>(),
            Artwork = new List<Artwork>(),
            Guids = GuidsFromProviderIds(item.ProviderIds),
            Directors = Optional(item.People).Flatten().Collect(r => ProjectToDirector(r)).ToList(),
            Writers = Optional(item.People).Flatten().Collect(r => ProjectToWriter(r)).ToList()
        };

        if (item.IndexNumber.HasValue)
        {
            metadata.EpisodeNumber = item.IndexNumber.Value;
        }

        if (DateTime.TryParse(item.PremiereDate, out DateTime releaseDate))
        {
            metadata.ReleaseDate = releaseDate;
        }

        if (!string.IsNullOrWhiteSpace(item.ImageTags.Primary))
        {
            var thumbnail = new Artwork
            {
                ArtworkKind = ArtworkKind.Thumbnail,
                Path = $"jellyfin://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(thumbnail);
        }

        return metadata;
    }

    private List<MetadataGuid> GuidsFromProviderIds(JellyfinProviderIdsResponse providerIds)
    {
        var result = new List<MetadataGuid>();

        if (providerIds != null)
        {
            if (!string.IsNullOrWhiteSpace(providerIds.Imdb))
            {
                result.Add(new MetadataGuid { Guid = $"imdb://{providerIds.Imdb}" });
            }

            if (!string.IsNullOrWhiteSpace(providerIds.Tmdb))
            {
                result.Add(new MetadataGuid { Guid = $"tmdb://{providerIds.Tmdb}" });
            }

            if (!string.IsNullOrWhiteSpace(providerIds.Tvdb))
            {
                result.Add(new MetadataGuid { Guid = $"tvdb://{providerIds.Tvdb}" });
            }
        }

        return result;
    }
}
