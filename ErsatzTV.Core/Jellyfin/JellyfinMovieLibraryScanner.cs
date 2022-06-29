using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Jellyfin;

public class JellyfinMovieLibraryScanner :
    MediaServerMovieLibraryScanner<JellyfinConnectionParameters, JellyfinLibrary, JellyfinMovie, JellyfinItemEtag>,
    IJellyfinMovieLibraryScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IJellyfinMovieRepository _jellyfinMovieRepository;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinPathReplacementService _pathReplacementService;

    public JellyfinMovieLibraryScanner(
        IJellyfinApiClient jellyfinApiClient,
        ISearchIndex searchIndex,
        IMediator mediator,
        IJellyfinMovieRepository jellyfinMovieRepository,
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IJellyfinPathReplacementService pathReplacementService,
        IMediaSourceRepository mediaSourceRepository,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<JellyfinMovieLibraryScanner> logger)
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
        _jellyfinApiClient = jellyfinApiClient;
        _jellyfinMovieRepository = jellyfinMovieRepository;
        _pathReplacementService = pathReplacementService;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        List<JellyfinPathReplacement> pathReplacements =
            await _mediaSourceRepository.GetJellyfinPathReplacements(library.MediaSourceId);

        string GetLocalPath(JellyfinMovie movie)
        {
            return _pathReplacementService.GetReplacementJellyfinPath(
                pathReplacements,
                movie.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _jellyfinMovieRepository,
            new JellyfinConnectionParameters(address, apiKey, library.MediaSourceId),
            library,
            GetLocalPath,
            ffmpegPath,
            ffprobePath,
            false,
            cancellationToken);
    }

    protected override string MediaServerItemId(JellyfinMovie movie) => movie.ItemId;

    protected override string MediaServerEtag(JellyfinMovie movie) => movie.Etag;

    protected override Task<Either<BaseError, int>> CountMovieLibraryItems(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library) =>
        _jellyfinApiClient.GetLibraryItemCount(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
            library.ItemId,
            JellyfinItemType.Movie,
            true);

    protected override IAsyncEnumerable<JellyfinMovie> GetMovieLibraryItems(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library) =>
        _jellyfinApiClient.GetMovieLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library);

    protected override Task<Option<MovieMetadata>> GetFullMetadata(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinMovie> result,
        JellyfinMovie incoming,
        bool deepScan) =>
        Task.FromResult<Option<MovieMetadata>>(None);

    protected override Task<Either<BaseError, MediaItemScanResult<JellyfinMovie>>> UpdateMetadata(
        MediaItemScanResult<JellyfinMovie> result,
        MovieMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<JellyfinMovie>>>(result);
}
