using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex;

public class PlexTelevisionLibraryScanner :
    MediaServerTelevisionLibraryScanner<PlexConnectionParameters, PlexLibrary, PlexShow, PlexSeason, PlexEpisode,
        PlexItemEtag>, IPlexTelevisionLibraryScanner
{
    private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly IPlexServerApiClient _plexServerApiClient;
    private readonly IPlexTelevisionRepository _plexTelevisionRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public PlexTelevisionLibraryScanner(
        IPlexServerApiClient plexServerApiClient,
        ITelevisionRepository televisionRepository,
        IMetadataRepository metadataRepository,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IPlexTelevisionRepository plexTelevisionRepository,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<PlexTelevisionLibraryScanner> logger)
        : base(
            localStatisticsProvider,
            localSubtitlesProvider,
            localFileSystem,
            searchRepository,
            searchIndex,
            mediator,
            logger)
    {
        _plexServerApiClient = plexServerApiClient;
        _televisionRepository = televisionRepository;
        _metadataRepository = metadataRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _plexTelevisionRepository = plexTelevisionRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<PlexPathReplacement> pathReplacements =
            await _mediaSourceRepository.GetPlexPathReplacements(library.MediaSourceId);

        string GetLocalPath(PlexEpisode episode)
        {
            return _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                episode.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _plexTelevisionRepository,
            new PlexConnectionParameters(connection, token),
            library,
            GetLocalPath,
            ffmpegPath,
            ffprobePath,
            deepScan,
            cancellationToken);
    }

    // TODO: add or remove metadata?
    // private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateMetadata(
    //     MediaItemScanResult<PlexEpisode> result,
    //     PlexEpisode incoming)
    // {
    //     PlexEpisode existing = result.Item;
    //
    //     var toUpdate = existing.EpisodeMetadata
    //         .Where(em => incoming.EpisodeMetadata.Any(em2 => em2.EpisodeNumber == em.EpisodeNumber))
    //         .ToList();
    //     var toRemove = existing.EpisodeMetadata.Except(toUpdate).ToList();
    //     var toAdd = incoming.EpisodeMetadata
    //         .Where(em => existing.EpisodeMetadata.All(em2 => em2.EpisodeNumber != em.EpisodeNumber))
    //         .ToList();
    //
    //     foreach (EpisodeMetadata metadata in toRemove)
    //     {
    //         await _televisionRepository.RemoveMetadata(existing, metadata);
    //     }
    //
    //     foreach (EpisodeMetadata metadata in toAdd)
    //     {
    //         metadata.EpisodeId = existing.Id;
    //         metadata.Episode = existing;
    //         existing.EpisodeMetadata.Add(metadata);
    //
    //         await _metadataRepository.Add(metadata);
    //     }
    //
    //     // TODO: update existing metadata
    //
    //     return result;
    // }

    // foreach (MediaFile incomingFile in incomingVersion.MediaFiles.HeadOrNone())
    // {
    //     foreach (MediaFile existingFile in existingVersion.MediaFiles.HeadOrNone())
    //     {
    //         if (incomingFile.Path != existingFile.Path)
    //         {
    //             _logger.LogDebug(
    //                 "Plex episode has moved from {OldPath} to {NewPath}",
    //                 existingFile.Path,
    //                 incomingFile.Path);
    //
    //             existingFile.Path = incomingFile.Path;
    //
    //             await _televisionRepository.UpdatePath(existingFile.Id, incomingFile.Path);
    //         }
    //     }
    // }

    protected override Task<Either<BaseError, int>> CountShowLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library) =>
        _plexServerApiClient.GetLibraryItemCount(
            library,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override IAsyncEnumerable<PlexShow> GetShowLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library) =>
        _plexServerApiClient.GetShowLibraryContents(
            library,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override Task<Either<BaseError, int>> CountSeasonLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        PlexShow show) =>
        _plexServerApiClient.CountShowSeasons(
            show,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override IAsyncEnumerable<PlexSeason> GetSeasonLibraryItems(
        PlexLibrary library,
        PlexConnectionParameters connectionParameters,
        PlexShow show) =>
        _plexServerApiClient.GetShowSeasons(
            library,
            show,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override Task<Either<BaseError, int>> CountEpisodeLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        PlexSeason season) =>
        _plexServerApiClient.CountSeasonEpisodes(
            season,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override IAsyncEnumerable<PlexEpisode> GetEpisodeLibraryItems(
        PlexLibrary library,
        PlexConnectionParameters connectionParameters,
        PlexShow _,
        PlexSeason season) =>
        _plexServerApiClient.GetSeasonEpisodes(
            library,
            season,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override async Task<Option<ShowMetadata>> GetFullMetadata(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        MediaItemScanResult<PlexShow> result,
        PlexShow incoming,
        bool deepScan)
    {
        if (result.IsAdded || result.Item.Etag != incoming.Etag || deepScan)
        {
            Either<BaseError, ShowMetadata> maybeMetadata = await _plexServerApiClient.GetShowMetadata(
                library,
                incoming.Key.Replace("/children", string.Empty).Split("/").Last(),
                connectionParameters.Connection,
                connectionParameters.Token);

            foreach (BaseError error in maybeMetadata.LeftToSeq())
            {
                _logger.LogWarning("Failed to get show metadata from Plex: {Error}", error.ToString());
            }

            return maybeMetadata.ToOption();
        }

        return None;
    }

    protected override Task<Option<SeasonMetadata>> GetFullMetadata(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        MediaItemScanResult<PlexSeason> result,
        PlexSeason incoming,
        bool deepScan)
    {
        if (result.IsAdded || result.Item.Etag != incoming.Etag || deepScan)
        {
            return incoming.SeasonMetadata.HeadOrNone().AsTask();
        }

        return Option<SeasonMetadata>.None.AsTask();
    }

    protected override async Task<Option<EpisodeMetadata>> GetFullMetadata(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        MediaItemScanResult<PlexEpisode> result,
        PlexEpisode incoming,
        bool deepScan)
    {
        if (result.IsAdded || result.Item.Etag != incoming.Etag || deepScan)
        {
            Either<BaseError, EpisodeMetadata> maybeMetadata =
                await _plexServerApiClient.GetEpisodeMetadataAndStatistics(
                        library,
                        incoming.Key.Split("/").Last(),
                        connectionParameters.Connection,
                        connectionParameters.Token)
                    .MapT(tuple => tuple.Item1); // drop the statistics part from plex, we scan locally

            foreach (BaseError error in maybeMetadata.LeftToSeq())
            {
                _logger.LogWarning("Failed to get episode metadata from Plex: {Error}", error.ToString());
            }

            return maybeMetadata.ToOption();
        }

        return None;
    }

    protected override async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> UpdateMetadata(
        MediaItemScanResult<PlexShow> result,
        ShowMetadata fullMetadata)
    {
        PlexShow existing = result.Item;
        ShowMetadata existingMetadata = existing.ShowMetadata.Head();

        if (existingMetadata.MetadataKind != MetadataKind.External)
        {
            existingMetadata.MetadataKind = MetadataKind.External;
            await _metadataRepository.MarkAsExternal(existingMetadata);
        }

        if (existingMetadata.ContentRating != fullMetadata.ContentRating)
        {
            existingMetadata.ContentRating = fullMetadata.ContentRating;
            await _metadataRepository.SetContentRating(existingMetadata, fullMetadata.ContentRating);
            result.IsUpdated = true;
        }

        foreach (Genre genre in existingMetadata.Genres
                     .Filter(g => fullMetadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Genres.Remove(genre);
            if (await _metadataRepository.RemoveGenre(genre))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Genre genre in fullMetadata.Genres
                     .Filter(g => existingMetadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Genres.Add(genre);
            if (await _televisionRepository.AddGenre(existingMetadata, genre))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Studio studio in existingMetadata.Studios
                     .Filter(s => fullMetadata.Studios.All(s2 => s2.Name != s.Name))
                     .ToList())
        {
            existingMetadata.Studios.Remove(studio);
            if (await _metadataRepository.RemoveStudio(studio))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Studio studio in fullMetadata.Studios
                     .Filter(s => existingMetadata.Studios.All(s2 => s2.Name != s.Name))
                     .ToList())
        {
            existingMetadata.Studios.Add(studio);
            if (await _televisionRepository.AddStudio(existingMetadata, studio))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Actor actor in existingMetadata.Actors
                     .Filter(
                         a => fullMetadata.Actors.All(
                             a2 => a2.Name != a.Name || a.Artwork == null && a2.Artwork != null))
                     .ToList())
        {
            existingMetadata.Actors.Remove(actor);
            if (await _metadataRepository.RemoveActor(actor))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Actor actor in fullMetadata.Actors
                     .Filter(a => existingMetadata.Actors.All(a2 => a2.Name != a.Name))
                     .ToList())
        {
            existingMetadata.Actors.Add(actor);
            if (await _televisionRepository.AddActor(existingMetadata, actor))
            {
                result.IsUpdated = true;
            }
        }

        foreach (MetadataGuid guid in existingMetadata.Guids
                     .Filter(g => fullMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Remove(guid);
            if (await _metadataRepository.RemoveGuid(guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (MetadataGuid guid in fullMetadata.Guids
                     .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Add(guid);
            if (await _metadataRepository.AddGuid(existingMetadata, guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in existingMetadata.Tags
                     .Filter(g => fullMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Remove(tag);
            if (await _metadataRepository.RemoveTag(tag))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in fullMetadata.Tags
                     .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Add(tag);
            if (await _televisionRepository.AddTag(existingMetadata, tag))
            {
                result.IsUpdated = true;
            }
        }

        bool poster = await UpdateArtworkIfNeeded(existingMetadata, fullMetadata, ArtworkKind.Poster);
        bool fanArt = await UpdateArtworkIfNeeded(existingMetadata, fullMetadata, ArtworkKind.FanArt);
        if (poster || fanArt)
        {
            result.IsUpdated = true;
        }

        if (result.IsUpdated)
        {
            await _metadataRepository.MarkAsUpdated(existingMetadata, fullMetadata.DateUpdated);
        }

        return result;
    }

    protected override async Task<Either<BaseError, MediaItemScanResult<PlexSeason>>> UpdateMetadata(
        MediaItemScanResult<PlexSeason> result,
        SeasonMetadata fullMetadata)
    {
        PlexSeason existing = result.Item;
        SeasonMetadata existingMetadata = existing.SeasonMetadata.Head();

        foreach (MetadataGuid guid in existingMetadata.Guids
                     .Filter(g => fullMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Remove(guid);
            if (await _metadataRepository.RemoveGuid(guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (MetadataGuid guid in fullMetadata.Guids
                     .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Add(guid);
            if (await _metadataRepository.AddGuid(existingMetadata, guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in existingMetadata.Tags
                     .Filter(g => fullMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Remove(tag);
            if (await _metadataRepository.RemoveTag(tag))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in fullMetadata.Tags
                     .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Add(tag);
            if (await _televisionRepository.AddTag(existingMetadata, tag))
            {
                result.IsUpdated = true;
            }
        }

        if (await UpdateArtworkIfNeeded(existingMetadata, fullMetadata, ArtworkKind.Poster))
        {
            result.IsUpdated = true;
        }

        await _metadataRepository.MarkAsUpdated(existingMetadata, fullMetadata.DateUpdated);

        return result;
    }

    protected override async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateMetadata(
        MediaItemScanResult<PlexEpisode> result,
        EpisodeMetadata fullMetadata)
    {
        PlexEpisode existing = result.Item;
        EpisodeMetadata existingMetadata = existing.EpisodeMetadata.Head();

        foreach (MetadataGuid guid in existingMetadata.Guids
                     .Filter(g => fullMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Remove(guid);
            if (await _metadataRepository.RemoveGuid(guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (MetadataGuid guid in fullMetadata.Guids
                     .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            existingMetadata.Guids.Add(guid);
            if (await _metadataRepository.AddGuid(existingMetadata, guid))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in existingMetadata.Tags
                     .Filter(g => fullMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Remove(tag);
            if (await _metadataRepository.RemoveTag(tag))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Tag tag in fullMetadata.Tags
                     .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Tags.Add(tag);
            if (await _televisionRepository.AddTag(existingMetadata, tag))
            {
                result.IsUpdated = true;
            }
        }

        if (await UpdateArtworkIfNeeded(existingMetadata, fullMetadata, ArtworkKind.Thumbnail))
        {
            result.IsUpdated = true;
        }

        await _metadataRepository.MarkAsUpdated(existingMetadata, fullMetadata.DateUpdated);

        return result;
    }

    protected override string MediaServerItemId(PlexShow show) => show.Key;
    protected override string MediaServerItemId(PlexSeason season) => season.Key;
    protected override string MediaServerItemId(PlexEpisode episode) => episode.Key;

    protected override string MediaServerEtag(PlexShow show) => show.Etag;
    protected override string MediaServerEtag(PlexSeason season) => season.Etag;
    protected override string MediaServerEtag(PlexEpisode episode) => episode.Etag;

    private async Task<bool> UpdateArtworkIfNeeded(
        Domain.Metadata existingMetadata,
        Domain.Metadata incomingMetadata,
        ArtworkKind artworkKind)
    {
        if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
        {
            Option<Artwork> maybeIncomingArtwork = Optional(incomingMetadata.Artwork).Flatten()
                .Find(a => a.ArtworkKind == artworkKind);

            if (maybeIncomingArtwork.IsNone)
            {
                existingMetadata.Artwork ??= new List<Artwork>();
                existingMetadata.Artwork.RemoveAll(a => a.ArtworkKind == artworkKind);
                await _metadataRepository.RemoveArtwork(existingMetadata, artworkKind);
            }

            foreach (Artwork incomingArtwork in maybeIncomingArtwork)
            {
                _logger.LogDebug("Refreshing Plex {Attribute} from {Path}", artworkKind, incomingArtwork.Path);

                Option<Artwork> maybeExistingArtwork = Optional(existingMetadata.Artwork).Flatten()
                    .Find(a => a.ArtworkKind == artworkKind);

                if (maybeExistingArtwork.IsNone)
                {
                    existingMetadata.Artwork ??= new List<Artwork>();
                    existingMetadata.Artwork.Add(incomingArtwork);
                    await _metadataRepository.AddArtwork(existingMetadata, incomingArtwork);
                }

                foreach (Artwork existingArtwork in maybeExistingArtwork)
                {
                    existingArtwork.Path = incomingArtwork.Path;
                    existingArtwork.DateUpdated = incomingArtwork.DateUpdated;
                    await _metadataRepository.UpdateArtworkPath(existingArtwork);
                }
            }

            return true;
        }

        return false;
    }
}
