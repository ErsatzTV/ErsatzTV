﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Emby.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;

namespace ErsatzTV.Infrastructure.Emby;

public class EmbyApiClient : IEmbyApiClient
{
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ILogger<EmbyApiClient> _logger;
    private readonly IMemoryCache _memoryCache;

    public EmbyApiClient(
        IFallbackMetadataProvider fallbackMetadataProvider,
        IMemoryCache memoryCache,
        IEmbyPathReplacementService embyPathReplacementService,
        ILogger<EmbyApiClient> logger)
    {
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _memoryCache = memoryCache;
        _embyPathReplacementService = embyPathReplacementService;
        _logger = logger;
    }

    public async Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(
        string address,
        string apiKey)
    {
        try
        {
            IEmbyApi service = RestService.For<IEmbyApi>(address);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            return await service.GetSystemInformation(apiKey, cts.Token)
                .Map(response => new EmbyServerInformation(response.ServerName, response.OperatingSystem));
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout getting emby server name");
            return BaseError.New("Emby did not respond in time");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emby server name");
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey)
    {
        try
        {
            IEmbyApi service = RestService.For<IEmbyApi>(address);
            List<EmbyLibraryResponse> libraries = await service.GetLibraries(apiKey);
            return libraries
                .Map(Project)
                .Somes()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emby libraries");
            return BaseError.New(ex.Message);
        }
    }

    public IAsyncEnumerable<EmbyMovie> GetMovieLibraryItems(string address, string apiKey, EmbyLibrary library)
        => GetPagedLibraryContents(
            address,
            apiKey,
            library,
            library.ItemId,
            EmbyItemType.Movie,
            (service, itemId, skip, pageSize) => service.GetMovieLibraryItems(
                apiKey,
                itemId,
                startIndex: skip,
                limit: pageSize),
            (maybeLibrary, item) => maybeLibrary.Map(lib => ProjectToMovie(lib, item)).Flatten());

    public IAsyncEnumerable<EmbyShow> GetShowLibraryItems(string address, string apiKey, EmbyLibrary library)
        => GetPagedLibraryContents(
            address,
            apiKey,
            library,
            library.ItemId,
            EmbyItemType.Show,
            (service, itemId, skip, pageSize) => service.GetShowLibraryItems(
                apiKey,
                itemId,
                startIndex: skip,
                limit: pageSize),
            (_, item) => ProjectToShow(item));

    public IAsyncEnumerable<EmbySeason> GetSeasonLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId) => GetPagedLibraryContents(
        address,
        apiKey,
        library,
        showId,
        EmbyItemType.Season,
        (service, itemId, skip, pageSize) => service.GetSeasonLibraryItems(
            apiKey,
            itemId,
            startIndex: skip,
            limit: pageSize),
        (_, item) => ProjectToSeason(item));

    public IAsyncEnumerable<EmbyEpisode> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId,
        string seasonId) => GetPagedLibraryContents(
        address,
        apiKey,
        library,
        seasonId,
        EmbyItemType.Episode,
        (service, _, skip, pageSize) => service.GetEpisodeLibraryItems(
            apiKey,
            showId,
            seasonId,
            startIndex: skip,
            limit: pageSize),
        (maybeLibrary, item) => maybeLibrary.Map(lib => ProjectToEpisode(lib, item)).Flatten());

    public IAsyncEnumerable<EmbyCollection> GetCollectionLibraryItems(string address, string apiKey)
    {
        // TODO: should we enumerate collection libraries here?

        if (_memoryCache.TryGetValue("emby_collections_library_item_id", out string itemId))
        {
            return GetPagedLibraryContents(
                address,
                apiKey,
                None,
                itemId,
                EmbyItemType.Collection,
                (service, _, skip, pageSize) => service.GetCollectionLibraryItems(
                    apiKey,
                    itemId,
                    startIndex: skip,
                    limit: pageSize),
                (_, item) => ProjectToCollection(item));
        }

        return AsyncEnumerable.Empty<EmbyCollection>();
    }

    public IAsyncEnumerable<MediaItem> GetCollectionItems(
        string address,
        string apiKey,
        string collectionId) =>
        GetPagedLibraryContents(
            address,
            apiKey,
            None,
            collectionId,
            EmbyItemType.CollectionItems,
            (service, _, skip, pageSize) => service.GetCollectionItems(
                apiKey,
                collectionId,
                startIndex: skip,
                limit: pageSize),
            (_, item) => ProjectToCollectionMediaItem(item));

    public async Task<Either<BaseError, int>> GetLibraryItemCount(
        string address,
        string apiKey,
        string parentId,
        string includeItemTypes)
    {
        try
        {
            IEmbyApi service = RestService.For<IEmbyApi>(address);
            EmbyLibraryItemsResponse items = await service.GetLibraryStats(apiKey, parentId, includeItemTypes);
            return items.TotalRecordCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Emby library item count");
            return BaseError.New(ex.Message);
        }
    }

    private static async IAsyncEnumerable<TItem> GetPagedLibraryContents<TItem>(
        string address,
        string apiKey,
        Option<EmbyLibrary> maybeLibrary,
        string parentId,
        string itemType,
        Func<IEmbyApi, string, int, int, Task<EmbyLibraryItemsResponse>> getItems,
        Func<Option<EmbyLibrary>, EmbyLibraryItemResponse, Option<TItem>> mapper)
    {
        IEmbyApi service = RestService.For<IEmbyApi>(address);
        int size = await service
            .GetLibraryStats(apiKey, parentId, itemType)
            .Map(r => r.TotalRecordCount);

        const int PAGE_SIZE = 10;

        int pages = (size - 1) / PAGE_SIZE + 1;

        for (var i = 0; i < pages; i++)
        {
            int skip = i * PAGE_SIZE;

            Task<IEnumerable<TItem>> result = getItems(service, parentId, skip, PAGE_SIZE)
                .Map(items => items.Items.Map(item => mapper(maybeLibrary, item)).Somes());

#pragma warning disable VSTHRD003
            foreach (TItem item in await result)
#pragma warning restore VSTHRD003
            {
                yield return item;
            }
        }
    }

    private Option<EmbyCollection> ProjectToCollection(EmbyLibraryItemResponse item)
    {
        try
        {
            return new EmbyCollection
            {
                ItemId = item.Id,
                Etag = item.Etag,
                Name = item.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Emby collection");
            return None;
        }
    }

    private Option<MediaItem> ProjectToCollectionMediaItem(EmbyLibraryItemResponse item)
    {
        try
        {
            return item.Type switch
            {
                "Movie" => new EmbyMovie { ItemId = item.Id },
                "Series" => new EmbyShow { ItemId = item.Id },
                "Season" => new EmbySeason { ItemId = item.Id },
                "Episode" => new EmbyEpisode { ItemId = item.Id },
                _ => None
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error projecting Emby collection media item");
            return None;
        }
    }

    private Option<EmbyLibrary> Project(EmbyLibraryResponse response) =>
        response.CollectionType?.ToLowerInvariant() switch
        {
            "tvshows" => new EmbyLibrary
            {
                ItemId = response.ItemId,
                Name = response.Name,
                MediaKind = LibraryMediaKind.Shows,
                ShouldSyncItems = false,
                Paths = new List<LibraryPath> { new() { Path = $"emby://{response.ItemId}" } },
                PathInfos = GetPathInfos(response)
            },
            "movies" => new EmbyLibrary
            {
                ItemId = response.ItemId,
                Name = response.Name,
                MediaKind = LibraryMediaKind.Movies,
                ShouldSyncItems = false,
                Paths = new List<LibraryPath> { new() { Path = $"emby://{response.ItemId}" } },
                PathInfos = GetPathInfos(response)
            },
            // TODO: ??? for music libraries
            "boxsets" => CacheCollectionLibraryId(response.ItemId),
            _ => None
        };

    private List<EmbyPathInfo> GetPathInfos(EmbyLibraryResponse response) =>
        response.LibraryOptions.PathInfos
            .Filter(pi => !string.IsNullOrWhiteSpace(pi.NetworkPath))
            .Map(
                pi => new EmbyPathInfo
                {
                    Path = pi.Path,
                    NetworkPath = pi.NetworkPath
                }).ToList();

    private Option<EmbyLibrary> CacheCollectionLibraryId(string itemId)
    {
        _memoryCache.Set("emby_collections_library_item_id", itemId);
        return None;
    }

    private Option<EmbyMovie> ProjectToMovie(EmbyLibrary library, EmbyLibraryItemResponse item)
    {
        try
        {
            if (item.MediaSources.Any(ms => ms.Protocol != "File"))
            {
                return None;
            }

            string path = item.Path ?? string.Empty;
            foreach (EmbyPathInfo pathInfo in
                     library.PathInfos.Filter(pi => !string.IsNullOrWhiteSpace(pi.NetworkPath)))
            {
                if (path.StartsWith(pathInfo.NetworkPath, StringComparison.Ordinal))
                {
                    path = _embyPathReplacementService.ReplaceNetworkPath(
                        (EmbyMediaSource)library.MediaSource,
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

            var movie = new EmbyMovie
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
            _logger.LogWarning(ex, "Error projecting Emby movie");
            return None;
        }
    }

    private MovieMetadata ProjectToMovieMetadata(EmbyLibraryItemResponse item)
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
                Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(poster);
        }

        if (item.BackdropImageTags.Any())
        {
            var fanArt = new Artwork
            {
                ArtworkKind = ArtworkKind.FanArt,
                Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(fanArt);
        }

        return metadata;
    }

    private Option<Actor> ProjectToActor(EmbyPersonResponse person, DateTime dateAdded)
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
                Path = $"emby://Items/{person.Id}/Images/Primary?tag={person.PrimaryImageTag}",
                ArtworkKind = ArtworkKind.Thumbnail,
                DateAdded = dateAdded
            };
        }

        return actor;
    }

    private static Option<Director> ProjectToDirector(EmbyPersonResponse person)
    {
        if (person.Type?.ToLowerInvariant() != "director")
        {
            return None;
        }

        return new Director { Name = person.Name };
    }

    private static Option<Writer> ProjectToWriter(EmbyPersonResponse person)
    {
        if (person.Type?.ToLowerInvariant() != "writer")
        {
            return None;
        }

        return new Writer { Name = person.Name };
    }

    private Option<EmbyShow> ProjectToShow(EmbyLibraryItemResponse item)
    {
        try
        {
            ShowMetadata metadata = ProjectToShowMetadata(item);

            var show = new EmbyShow
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
            _logger.LogWarning(ex, "Error projecting Emby show");
            return None;
        }
    }

    private ShowMetadata ProjectToShowMetadata(EmbyLibraryItemResponse item)
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
                Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(poster);
        }

        if (!string.IsNullOrWhiteSpace(item.ImageTags.Thumb))
        {
            var thumb = new Artwork
            {
                ArtworkKind = ArtworkKind.Thumbnail,
                Path = $"emby://Items/{item.Id}/Images/Thumb?tag={item.ImageTags.Thumb}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(thumb);
        }

        if (item.BackdropImageTags.Any())
        {
            var fanArt = new Artwork
            {
                ArtworkKind = ArtworkKind.FanArt,
                Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(fanArt);
        }

        return metadata;
    }

    private Option<EmbySeason> ProjectToSeason(EmbyLibraryItemResponse item)
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
                    Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(poster);
            }

            if (item.BackdropImageTags.Any())
            {
                var fanArt = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = $"emby://Items/{item.Id}/Images/Backdrop?tag={item.BackdropImageTags.Head()}",
                    DateAdded = dateAdded
                };
                metadata.Artwork.Add(fanArt);
            }

            var season = new EmbySeason
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
            _logger.LogWarning(ex, "Error projecting Emby show");
            return None;
        }
    }

    private Option<EmbyEpisode> ProjectToEpisode(EmbyLibrary library, EmbyLibraryItemResponse item)
    {
        try
        {
            if (item.LocationType == "Virtual")
            {
                return None;
            }

            string path = item.Path ?? string.Empty;
            foreach (EmbyPathInfo pathInfo in
                     library.PathInfos.Filter(pi => !string.IsNullOrWhiteSpace(pi.NetworkPath)))
            {
                if (path.StartsWith(pathInfo.NetworkPath, StringComparison.Ordinal))
                {
                    path = _embyPathReplacementService.ReplaceNetworkPath(
                        (EmbyMediaSource)library.MediaSource,
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

            var episode = new EmbyEpisode
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
            _logger.LogWarning(ex, "Error projecting Emby movie");
            return None;
        }
    }

    private EpisodeMetadata ProjectToEpisodeMetadata(EmbyLibraryItemResponse item)
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
                Path = $"emby://Items/{item.Id}/Images/Primary?tag={item.ImageTags.Primary}",
                DateAdded = dateAdded
            };
            metadata.Artwork.Add(thumbnail);
        }

        return metadata;
    }

    private List<MetadataGuid> GuidsFromProviderIds(EmbyProviderIdsResponse providerIds)
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
