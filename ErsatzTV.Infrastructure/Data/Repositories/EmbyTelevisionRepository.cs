﻿using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class EmbyTelevisionRepository : IEmbyTelevisionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILogger<EmbyTelevisionRepository> _logger;

    public EmbyTelevisionRepository(
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<EmbyTelevisionRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<List<EmbyItemEtag>> GetExistingShows(EmbyLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<EmbyItemEtag>(
                @"SELECT ItemId, Etag, MI.State FROM EmbyShow
                      INNER JOIN `Show` S on EmbyShow.Id = S.Id
                      INNER JOIN MediaItem MI on S.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

    public async Task<List<EmbyItemEtag>> GetExistingSeasons(EmbyLibrary library, EmbyShow show)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<EmbyItemEtag>(
                @"SELECT EmbySeason.ItemId, EmbySeason.Etag, MI.State FROM EmbySeason
                      INNER JOIN Season S on EmbySeason.Id = S.Id
                      INNER JOIN MediaItem MI on S.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      INNER JOIN `Show` S2 on S.ShowId = S2.Id
                      INNER JOIN EmbyShow JS on S2.Id = JS.Id
                      WHERE LP.LibraryId = @LibraryId AND JS.ItemId = @ShowItemId",
                new { LibraryId = library.Id, ShowItemId = show.ItemId })
            .Map(result => result.ToList());
    }

    public async Task<List<EmbyItemEtag>> GetExistingEpisodes(EmbyLibrary library, EmbySeason season)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<EmbyItemEtag>(
                @"SELECT EmbyEpisode.ItemId, EmbyEpisode.Etag, MI.State FROM EmbyEpisode
                      INNER JOIN Episode E on EmbyEpisode.Id = E.Id
                      INNER JOIN MediaItem MI on E.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      INNER JOIN Season S2 on E.SeasonId = S2.Id
                      INNER JOIN EmbySeason JS on S2.Id = JS.Id
                      WHERE LP.LibraryId = @LibraryId AND JS.ItemId = @SeasonItemId",
                new { LibraryId = library.Id, SeasonItemId = season.ItemId })
            .Map(result => result.ToList());
    }

    public async Task<Either<BaseError, MediaItemScanResult<EmbyShow>>> GetOrAdd(EmbyLibrary library, EmbyShow item)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<EmbyShow> maybeExisting = await dbContext.EmbyShows
            .Include(m => m.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Tags)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Actors)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(m => m.ShowMetadata)
            .ThenInclude(mm => mm.Guids)
            .Include(m => m.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(s => s.ItemId, s => s.ItemId == item.ItemId);

        foreach (EmbyShow embyShow in maybeExisting)
        {
            var result = new MediaItemScanResult<EmbyShow>(embyShow) { IsAdded = false };
            if (embyShow.Etag != item.Etag)
            {
                await UpdateShow(dbContext, embyShow, item);
                result.IsUpdated = true;
            }

            return result;
        }

        return await AddShow(dbContext, library, item);
    }

    public async Task<Either<BaseError, MediaItemScanResult<EmbySeason>>> GetOrAdd(EmbyLibrary library, EmbySeason item)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<EmbySeason> maybeExisting = await dbContext.EmbySeasons
            .Include(m => m.LibraryPath)
            .Include(m => m.SeasonMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(m => m.SeasonMetadata)
            .ThenInclude(mm => mm.Guids)
            .SelectOneAsync(s => s.ItemId, s => s.ItemId == item.ItemId);

        foreach (EmbySeason embySeason in maybeExisting)
        {
            var result = new MediaItemScanResult<EmbySeason>(embySeason) { IsAdded = false };
            if (embySeason.Etag != item.Etag)
            {
                await UpdateSeason(dbContext, embySeason, item);
                result.IsUpdated = true;
            }

            return result;
        }

        return await AddSeason(dbContext, library, item);
    }

    public async Task<Either<BaseError, MediaItemScanResult<EmbyEpisode>>> GetOrAdd(
        EmbyLibrary library,
        EmbyEpisode item,
        bool deepScan)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<EmbyEpisode> maybeExisting = await dbContext.EmbyEpisodes
            .Include(m => m.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Guids)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Tags)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Actors)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(m => m.EpisodeMetadata)
            .ThenInclude(mm => mm.Writers)
            .Include(m => m.Season)
            .Include(m => m.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(s => s.ItemId, s => s.ItemId == item.ItemId);

        foreach (EmbyEpisode embyEpisode in maybeExisting)
        {
            var result = new MediaItemScanResult<EmbyEpisode>(embyEpisode) { IsAdded = false };
            if (embyEpisode.Etag != item.Etag || deepScan)
            {
                await UpdateEpisode(dbContext, embyEpisode, item);
                result.IsUpdated = true;
            }

            return result;
        }

        return await AddEpisode(dbContext, library, item);
    }

    public async Task<Unit> SetEtag(EmbyShow show, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbyShow SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, show.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetEtag(EmbySeason season, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbySeason SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, season.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetEtag(EmbyEpisode episode, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbyEpisode SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, episode.Id }).Map(_ => Unit.Default);
    }

    public async Task<Option<int>> FlagNormal(EmbyLibrary library, EmbyEpisode episode)
    {
        if (episode.State is MediaItemState.Normal)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyEpisode.Id FROM EmbyEpisode
            INNER JOIN MediaItem MI ON MI.Id = EmbyEpisode.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE EmbyEpisode.ItemId = @ItemId",
            new { LibraryId = library.Id, episode.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 0 WHERE Id = @Id AND State != 0",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagNormal(EmbyLibrary library, EmbySeason season)
    {
        if (season.State is MediaItemState.Normal)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        season.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbySeason.Id FROM EmbySeason
            INNER JOIN MediaItem MI ON MI.Id = EmbySeason.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE EmbySeason.ItemId = @ItemId",
            new { LibraryId = library.Id, season.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 0 WHERE Id = @Id AND State != 0",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagNormal(EmbyLibrary library, EmbyShow show)
    {
        if (show.State is MediaItemState.Normal)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        show.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyShow.Id FROM EmbyShow
            INNER JOIN MediaItem MI ON MI.Id = EmbyShow.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE EmbyShow.ItemId = @ItemId",
            new { LibraryId = library.Id, show.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 0 WHERE Id = @Id AND State != 0",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<List<int>> FlagFileNotFoundShows(EmbyLibrary library, List<string> showItemIds)
    {
        if (showItemIds.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN EmbyShow ON EmbyShow.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE EmbyShow.ItemId IN @ShowItemIds",
                new { LibraryId = library.Id, ShowItemIds = showItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaItem SET State = 1 WHERE Id IN @Ids AND State != 1",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundSeasons(EmbyLibrary library, List<string> seasonItemIds)
    {
        if (seasonItemIds.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN EmbySeason ON EmbySeason.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE EmbySeason.ItemId IN @SeasonItemIds",
                new { LibraryId = library.Id, SeasonItemIds = seasonItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaItem SET State = 1 WHERE Id IN @Ids AND State != 1",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundEpisodes(EmbyLibrary library, List<string> episodeItemIds)
    {
        if (episodeItemIds.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN EmbyEpisode ON EmbyEpisode.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE EmbyEpisode.ItemId IN @EpisodeItemIds",
                new { LibraryId = library.Id, EpisodeItemIds = episodeItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaItem SET State = 1 WHERE Id IN @Ids AND State != 1",
            new { Ids = ids });

        return ids;
    }

    public async Task<Option<int>> FlagUnavailable(EmbyLibrary library, EmbyEpisode episode)
    {
        if (episode.State is MediaItemState.Unavailable)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.Unavailable;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyEpisode.Id FROM EmbyEpisode
              INNER JOIN MediaItem MI ON MI.Id = EmbyEpisode.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE EmbyEpisode.ItemId = @ItemId",
            new { LibraryId = library.Id, episode.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 2 WHERE Id = @Id AND State != 2",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagRemoteOnly(EmbyLibrary library, EmbyEpisode episode)
    {
        if (episode.State is MediaItemState.RemoteOnly)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.RemoteOnly;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyEpisode.Id FROM EmbyEpisode
              INNER JOIN MediaItem MI ON MI.Id = EmbyEpisode.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE EmbyEpisode.ItemId = @ItemId",
            new { LibraryId = library.Id, episode.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 3 WHERE Id = @Id AND State != 3",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    private static async Task UpdateShow(TvContext dbContext, EmbyShow existing, EmbyShow incoming)
    {
        // library path is used for search indexing later
        incoming.LibraryPath = existing.LibraryPath;
        incoming.Id = existing.Id;

        // metadata
        ShowMetadata metadata = existing.ShowMetadata.Head();
        ShowMetadata incomingMetadata = incoming.ShowMetadata.Head();
        metadata.MetadataKind = incomingMetadata.MetadataKind;
        metadata.ContentRating = incomingMetadata.ContentRating;
        metadata.Title = incomingMetadata.Title;
        metadata.SortTitle = incomingMetadata.SortTitle;
        metadata.Plot = incomingMetadata.Plot;
        metadata.Year = incomingMetadata.Year;
        metadata.Tagline = incomingMetadata.Tagline;
        metadata.DateAdded = incomingMetadata.DateAdded;
        metadata.DateUpdated = DateTime.UtcNow;

        // genres
        foreach (Genre genre in metadata.Genres
                     .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Remove(genre);
        }

        foreach (Genre genre in incomingMetadata.Genres
                     .Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Add(genre);
        }

        // tags
        foreach (Tag tag in metadata.Tags
                     .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .Filter(g => g.ExternalCollectionId is null)
                     .ToList())
        {
            metadata.Tags.Remove(tag);
        }

        foreach (Tag tag in incomingMetadata.Tags
                     .Filter(g => metadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Tags.Add(tag);
        }

        // studios
        foreach (Studio studio in metadata.Studios
                     .Filter(g => incomingMetadata.Studios.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Studios.Remove(studio);
        }

        foreach (Studio studio in incomingMetadata.Studios
                     .Filter(g => metadata.Studios.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Studios.Add(studio);
        }

        // actors
        foreach (Actor actor in metadata.Actors
                     .Filter(
                         a => incomingMetadata.Actors.All(
                             a2 => a2.Name != a.Name || a.Artwork == null && a2.Artwork != null))
                     .ToList())
        {
            metadata.Actors.Remove(actor);
        }

        foreach (Actor actor in incomingMetadata.Actors
                     .Filter(a => metadata.Actors.All(a2 => a2.Name != a.Name))
                     .ToList())
        {
            metadata.Actors.Add(actor);
        }

        // guids
        foreach (MetadataGuid guid in metadata.Guids
                     .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Remove(guid);
        }

        foreach (MetadataGuid guid in incomingMetadata.Guids
                     .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Add(guid);
        }

        metadata.ReleaseDate = incomingMetadata.ReleaseDate;

        // poster
        Artwork incomingPoster =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
        if (incomingPoster != null)
        {
            Artwork poster = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
            if (poster == null)
            {
                poster = new Artwork { ArtworkKind = ArtworkKind.Poster };
                metadata.Artwork.Add(poster);
            }

            poster.Path = incomingPoster.Path;
            poster.DateAdded = incomingPoster.DateAdded;
            poster.DateUpdated = incomingPoster.DateUpdated;
        }

        // fan art
        Artwork incomingFanArt =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
        if (incomingFanArt != null)
        {
            Artwork fanArt = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
            if (fanArt == null)
            {
                fanArt = new Artwork { ArtworkKind = ArtworkKind.FanArt };
                metadata.Artwork.Add(fanArt);
            }

            fanArt.Path = incomingFanArt.Path;
            fanArt.DateAdded = incomingFanArt.DateAdded;
            fanArt.DateUpdated = incomingFanArt.DateUpdated;
        }

        var paths = incomingMetadata.Artwork.Map(a => a.Path).ToList();
        foreach (Artwork artworkToRemove in metadata.Artwork
                     .Filter(a => !paths.Contains(a.Path))
                     .ToList())
        {
            metadata.Artwork.Remove(artworkToRemove);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpdateSeason(TvContext dbContext, EmbySeason existing, EmbySeason incoming)
    {
        // library path is used for search indexing later
        incoming.LibraryPath = existing.LibraryPath;
        incoming.Id = existing.Id;

        existing.SeasonNumber = incoming.SeasonNumber;

        // metadata
        SeasonMetadata metadata = existing.SeasonMetadata.Head();
        SeasonMetadata incomingMetadata = incoming.SeasonMetadata.Head();
        metadata.Title = incomingMetadata.Title;
        metadata.SortTitle = incomingMetadata.SortTitle;
        metadata.Year = incomingMetadata.Year;
        metadata.DateAdded = incomingMetadata.DateAdded;
        metadata.DateUpdated = DateTime.UtcNow;
        metadata.ReleaseDate = incomingMetadata.ReleaseDate;

        // poster
        Artwork incomingPoster =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
        if (incomingPoster != null)
        {
            Artwork poster = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
            if (poster == null)
            {
                poster = new Artwork { ArtworkKind = ArtworkKind.Poster };
                metadata.Artwork.Add(poster);
            }

            poster.Path = incomingPoster.Path;
            poster.DateAdded = incomingPoster.DateAdded;
            poster.DateUpdated = incomingPoster.DateUpdated;
        }

        // thumbnail
        Artwork incomingThumbnail =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
        if (incomingThumbnail != null)
        {
            Artwork thumb = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
            if (thumb == null)
            {
                thumb = new Artwork { ArtworkKind = ArtworkKind.Thumbnail };
                metadata.Artwork.Add(thumb);
            }

            thumb.Path = incomingThumbnail.Path;
            thumb.DateAdded = incomingThumbnail.DateAdded;
            thumb.DateUpdated = incomingThumbnail.DateUpdated;
        }

        // fan art
        Artwork incomingFanArt =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
        if (incomingFanArt != null)
        {
            Artwork fanArt = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
            if (fanArt == null)
            {
                fanArt = new Artwork { ArtworkKind = ArtworkKind.FanArt };
                metadata.Artwork.Add(fanArt);
            }

            fanArt.Path = incomingFanArt.Path;
            fanArt.DateAdded = incomingFanArt.DateAdded;
            fanArt.DateUpdated = incomingFanArt.DateUpdated;
        }

        // guids
        foreach (MetadataGuid guid in metadata.Guids
                     .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Remove(guid);
        }

        foreach (MetadataGuid guid in incomingMetadata.Guids
                     .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Add(guid);
        }

        var paths = incomingMetadata.Artwork.Map(a => a.Path).ToList();
        foreach (Artwork artworkToRemove in metadata.Artwork
                     .Filter(a => !paths.Contains(a.Path))
                     .ToList())
        {
            metadata.Artwork.Remove(artworkToRemove);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpdateEpisode(TvContext dbContext, EmbyEpisode existing, EmbyEpisode incoming)
    {
        // library path is used for search indexing later
        incoming.LibraryPath = existing.LibraryPath;
        incoming.Id = existing.Id;

        // metadata
        // TODO: multiple metadata?
        EpisodeMetadata metadata = existing.EpisodeMetadata.Head();
        EpisodeMetadata incomingMetadata = incoming.EpisodeMetadata.Head();
        metadata.Title = incomingMetadata.Title;
        metadata.SortTitle = incomingMetadata.SortTitle;
        metadata.Plot = incomingMetadata.Plot;
        metadata.Year = incomingMetadata.Year;
        metadata.DateAdded = incomingMetadata.DateAdded;
        metadata.DateUpdated = DateTime.UtcNow;
        metadata.ReleaseDate = incomingMetadata.ReleaseDate;
        metadata.EpisodeNumber = incomingMetadata.EpisodeNumber;

        // thumbnail
        Artwork incomingThumbnail =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
        if (incomingThumbnail != null)
        {
            Artwork thumbnail = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
            if (thumbnail == null)
            {
                thumbnail = new Artwork { ArtworkKind = ArtworkKind.Thumbnail };
                metadata.Artwork.Add(thumbnail);
            }

            thumbnail.Path = incomingThumbnail.Path;
            thumbnail.DateAdded = incomingThumbnail.DateAdded;
            thumbnail.DateUpdated = incomingThumbnail.DateUpdated;
        }

        // directors
        foreach (Director director in metadata.Directors
                     .Filter(d => incomingMetadata.Directors.All(d2 => d2.Name != d.Name))
                     .ToList())
        {
            metadata.Directors.Remove(director);
        }

        foreach (Director director in incomingMetadata.Directors
                     .Filter(d => metadata.Directors.All(d2 => d2.Name != d.Name))
                     .ToList())
        {
            metadata.Directors.Add(director);
        }

        // writers
        foreach (Writer writer in metadata.Writers
                     .Filter(w => incomingMetadata.Writers.All(w2 => w2.Name != w.Name))
                     .ToList())
        {
            metadata.Writers.Remove(writer);
        }

        foreach (Writer writer in incomingMetadata.Writers
                     .Filter(w => metadata.Writers.All(w2 => w2.Name != w.Name))
                     .ToList())
        {
            metadata.Writers.Add(writer);
        }

        // guids
        foreach (MetadataGuid guid in metadata.Guids
                     .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Remove(guid);
        }

        foreach (MetadataGuid guid in incomingMetadata.Guids
                     .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Add(guid);
        }

        // genres
        foreach (Genre genre in metadata.Genres
                     .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Remove(genre);
        }

        foreach (Genre genre in incomingMetadata.Genres
                     .Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Add(genre);
        }

        // tags
        foreach (Tag tag in metadata.Tags
                     .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .Filter(g => g.ExternalCollectionId is null)
                     .ToList())
        {
            metadata.Tags.Remove(tag);
        }

        foreach (Tag tag in incomingMetadata.Tags
                     .Filter(g => metadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Tags.Add(tag);
        }

        var paths = incomingMetadata.Artwork.Map(a => a.Path).ToList();
        foreach (Artwork artworkToRemove in metadata.Artwork
                     .Filter(a => !paths.Contains(a.Path))
                     .ToList())
        {
            metadata.Artwork.Remove(artworkToRemove);
        }

        // version
        MediaVersion version = existing.MediaVersions.Head();
        MediaVersion incomingVersion = incoming.MediaVersions.Head();
        version.Name = incomingVersion.Name;
        version.DateAdded = incomingVersion.DateAdded;
        version.Chapters = incomingVersion.Chapters;

        // media file
        MediaFile file = version.MediaFiles.Head();
        MediaFile incomingFile = incomingVersion.MediaFiles.Head();
        file.Path = incomingFile.Path;

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Either<BaseError, MediaItemScanResult<EmbyShow>>> AddShow(
        TvContext dbContext,
        EmbyLibrary library,
        EmbyShow show)
    {
        try
        {
            // blank out etag for initial save in case other updates fail
            string etag = show.Etag;
            show.Etag = string.Empty;

            show.LibraryPathId = library.Paths.Head().Id;

            await dbContext.AddAsync(show);
            await dbContext.SaveChangesAsync();

            // restore etag
            show.Etag = etag;

            await dbContext.Entry(show).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(show.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<EmbyShow>(show) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private static async Task<Either<BaseError, MediaItemScanResult<EmbySeason>>> AddSeason(
        TvContext dbContext,
        EmbyLibrary library,
        EmbySeason season)
    {
        try
        {
            // blank out etag for initial save in case other updates fail
            string etag = season.Etag;
            season.Etag = string.Empty;

            season.LibraryPathId = library.Paths.Head().Id;

            await dbContext.AddAsync(season);
            await dbContext.SaveChangesAsync();

            // restore etag
            season.Etag = etag;

            await dbContext.Entry(season).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(season.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<EmbySeason>(season) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<EmbyEpisode>>> AddEpisode(
        TvContext dbContext,
        EmbyLibrary library,
        EmbyEpisode episode)
    {
        try
        {
            if (await MediaItemRepository.MediaFileAlreadyExists(episode, library.Paths.Head().Id, dbContext, _logger))
            {
                return new MediaFileAlreadyExists();
            }

            // blank out etag for initial save in case other updates fail
            string etag = episode.Etag;
            episode.Etag = string.Empty;

            episode.LibraryPathId = library.Paths.Head().Id;

            await dbContext.AddAsync(episode);
            await dbContext.SaveChangesAsync();

            // restore etag
            episode.Etag = etag;

            await dbContext.Entry(episode).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(episode.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<EmbyEpisode>(episode) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
