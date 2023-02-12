using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using ErsatzTV.Scanner.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Emby;

public class EmbyMovieLibraryScanner :
    MediaServerMovieLibraryScanner<EmbyConnectionParameters, EmbyLibrary, EmbyMovie, EmbyItemEtag>,
    IEmbyMovieLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbyMovieRepository _embyMovieRepository;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyPathReplacementService _pathReplacementService;

    public EmbyMovieLibraryScanner(
        IEmbyApiClient embyApiClient,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyMovieRepository embyMovieRepository,
        IEmbyPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<EmbyMovieLibraryScanner> logger)
        : base(
            localStatisticsProvider,
            localSubtitlesProvider,
            localFileSystem,
            mediator,
            logger)
    {
        _embyApiClient = embyApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _embyMovieRepository = embyMovieRepository;
        _pathReplacementService = pathReplacementService;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<EmbyPathReplacement> pathReplacements =
            await _mediaSourceRepository.GetEmbyPathReplacements(library.MediaSourceId);

        string GetLocalPath(EmbyMovie movie)
        {
            return _pathReplacementService.GetReplacementEmbyPath(
                pathReplacements,
                movie.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _embyMovieRepository,
            new EmbyConnectionParameters(address, apiKey),
            library,
            GetLocalPath,
            ffmpegPath,
            ffprobePath,
            deepScan,
            cancellationToken);
    }

    protected override string MediaServerItemId(EmbyMovie movie) => movie.ItemId;
    protected override string MediaServerEtag(EmbyMovie movie) => movie.Etag;

    protected override Task<Either<BaseError, int>> CountMovieLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library) =>
        _embyApiClient.GetLibraryItemCount(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library.ItemId,
            EmbyItemType.Movie);

    protected override IAsyncEnumerable<EmbyMovie> GetMovieLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library) =>
        _embyApiClient.GetMovieLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library);

    protected override Task<Option<MovieMetadata>> GetFullMetadata(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyMovie> result,
        EmbyMovie incoming,
        bool deepScan) =>
        Task.FromResult<Option<MovieMetadata>>(None);

    protected override Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> UpdateMetadata(
        MediaItemScanResult<EmbyMovie> result,
        MovieMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbyMovie>>>(result);
}
