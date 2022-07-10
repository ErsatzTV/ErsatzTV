using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex;

public class PlexMovieLibraryScanner :
    MediaServerMovieLibraryScanner<PlexConnectionParameters, PlexLibrary, PlexMovie, PlexItemEtag>,
    IPlexMovieLibraryScanner
{
    private readonly ILogger<PlexMovieLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IPlexMovieRepository _plexMovieRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly IPlexServerApiClient _plexServerApiClient;

    public PlexMovieLibraryScanner(
        IPlexServerApiClient plexServerApiClient,
        IMovieRepository movieRepository,
        IMetadataRepository metadataRepository,
        ISearchIndex searchIndex,
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IPlexMovieRepository plexMovieRepository,
        IPlexPathReplacementService plexPathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<PlexMovieLibraryScanner> logger)
        : base(
            localStatisticsProvider,
            localSubtitlesProvider,
            localFileSystem,
            mediator,
            searchIndex,
            searchRepository,
            fallbackMetadataProvider,
            logger)
    {
        _plexServerApiClient = plexServerApiClient;
        _movieRepository = movieRepository;
        _metadataRepository = metadataRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexMovieRepository = plexMovieRepository;
        _plexPathReplacementService = plexPathReplacementService;
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

        string GetLocalPath(PlexMovie movie)
        {
            return _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                movie.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _plexMovieRepository,
            new PlexConnectionParameters(connection, token),
            library,
            GetLocalPath,
            ffmpegPath,
            ffprobePath,
            deepScan,
            cancellationToken);
    }

    protected override string MediaServerItemId(PlexMovie movie) => movie.Key;

    protected override string MediaServerEtag(PlexMovie movie) => movie.Etag;

    protected override Task<Either<BaseError, int>> CountMovieLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library)
        => _plexServerApiClient.GetLibraryItemCount(
            library,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override IAsyncEnumerable<PlexMovie> GetMovieLibraryItems(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library) =>
        _plexServerApiClient.GetMovieLibraryContents(
            library,
            connectionParameters.Connection,
            connectionParameters.Token);

    protected override async Task<Option<MovieMetadata>> GetFullMetadata(
        PlexConnectionParameters connectionParameters,
        PlexLibrary library,
        MediaItemScanResult<PlexMovie> result,
        PlexMovie incoming,
        bool deepScan)
    {
        if (result.IsAdded || result.Item.Etag != incoming.Etag || deepScan)
        {
            Either<BaseError, MovieMetadata> maybeMetadata = await _plexServerApiClient.GetMovieMetadata(
                library,
                incoming.Key.Split("/").Last(),
                connectionParameters.Connection,
                connectionParameters.Token);

            foreach (BaseError error in maybeMetadata.LeftToSeq())
            {
                _logger.LogWarning("Failed to get movie metadata from Plex: {Error}", error.ToString());
            }

            return maybeMetadata.ToOption();
        }

        return None;
    }

    protected override async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateMetadata(
        MediaItemScanResult<PlexMovie> result,
        MovieMetadata fullMetadata)
    {
        PlexMovie existing = result.Item;
        MovieMetadata existingMetadata = existing.MovieMetadata.Head();

        _logger.LogDebug(
            "Refreshing {Attribute} for {Title}",
            "Plex Metadata",
            existingMetadata.Title);

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
            if (await _movieRepository.AddGenre(existingMetadata, genre))
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
            if (await _movieRepository.AddStudio(existingMetadata, studio))
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
            if (await _movieRepository.AddActor(existingMetadata, actor))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Director director in existingMetadata.Directors
                     .Filter(g => fullMetadata.Directors.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Directors.Remove(director);
            if (await _metadataRepository.RemoveDirector(director))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Director director in fullMetadata.Directors
                     .Filter(g => existingMetadata.Directors.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Directors.Add(director);
            if (await _movieRepository.AddDirector(existingMetadata, director))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Writer writer in existingMetadata.Writers
                     .Filter(g => fullMetadata.Writers.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Writers.Remove(writer);
            if (await _metadataRepository.RemoveWriter(writer))
            {
                result.IsUpdated = true;
            }
        }

        foreach (Writer writer in fullMetadata.Writers
                     .Filter(g => existingMetadata.Writers.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            existingMetadata.Writers.Add(writer);
            if (await _movieRepository.AddWriter(existingMetadata, writer))
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
            if (await _movieRepository.AddTag(existingMetadata, tag))
            {
                result.IsUpdated = true;
            }
        }

        if (fullMetadata.SortTitle != existingMetadata.SortTitle)
        {
            existingMetadata.SortTitle = fullMetadata.SortTitle;
            if (await _movieRepository.UpdateSortTitle(existingMetadata))
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
