using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using ErsatzTV.Scanner.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Emby;

public class EmbyTelevisionLibraryScanner : MediaServerTelevisionLibraryScanner<EmbyConnectionParameters, EmbyLibrary,
    EmbyShow, EmbySeason, EmbyEpisode,
    EmbyItemEtag>, IEmbyTelevisionLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly ILogger<EmbyTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyPathReplacementService _pathReplacementService;
    private readonly IEmbyTelevisionRepository _televisionRepository;

    public EmbyTelevisionLibraryScanner(
        IScannerProxy scannerProxy,
        IEmbyApiClient embyApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyTelevisionRepository televisionRepository,
        IEmbyPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalChaptersProvider localChaptersProvider,
        IMetadataRepository metadataRepository,
        ILogger<EmbyTelevisionLibraryScanner> logger)
        : base(
            scannerProxy,
            localFileSystem,
            localChaptersProvider,
            metadataRepository,
            logger)
    {
        _embyApiClient = embyApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _televisionRepository = televisionRepository;
        _pathReplacementService = pathReplacementService;
        _logger = logger;
    }

    protected override bool ServerSupportsRemoteStreaming => true;

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<EmbyPathReplacement> pathReplacements =
            await _mediaSourceRepository.GetEmbyPathReplacements(library.MediaSourceId);

        string GetLocalPath(EmbyEpisode episode)
        {
            return _pathReplacementService.GetReplacementEmbyPath(
                pathReplacements,
                episode.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _televisionRepository,
            new EmbyConnectionParameters(address, apiKey),
            library,
            GetLocalPath,
            deepScan,
            cancellationToken);
    }

    public async Task<Either<BaseError, Unit>> ScanSingleShow(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId,
        string showTitle,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<EmbyPathReplacement> pathReplacements =
            await _mediaSourceRepository.GetEmbyPathReplacements(library.MediaSourceId);

        string GetLocalPath(EmbyEpisode episode)
        {
            return _pathReplacementService.GetReplacementEmbyPath(
                pathReplacements,
                episode.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        // Search for the specific show
        Either<BaseError, Option<EmbyShow>> searchResult = await _embyApiClient.GetSingleShow(
            address,
            apiKey,
            library,
            showId);

        return await searchResult.Match(
            async maybeShow =>
            {
                foreach (EmbyShow show in maybeShow)
                {
                    _logger.LogInformation(
                        "Found show '{ShowTitle}' with id {ShowId}, starting targeted scan",
                        showTitle,
                        show.ItemId);

                    return await ScanSingleShowInternal(
                        _televisionRepository,
                        new EmbyConnectionParameters(address, apiKey),
                        library,
                        show,
                        GetLocalPath,
                        deepScan,
                        cancellationToken);
                }

                _logger.LogWarning("No show found with id {ShowId} in library {LibraryName}", showId, library.Name);

                return Right<BaseError, Unit>(Unit.Default);
            },
            error => Task.FromResult<Either<BaseError, Unit>>(error));
    }

    protected override IAsyncEnumerable<Tuple<EmbyShow, int>> GetShowLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library) =>
        _embyApiClient.GetShowLibraryItems(connectionParameters.Address, connectionParameters.ApiKey, library);

    protected override string MediaServerItemId(EmbyShow show) => show.ItemId;
    protected override string MediaServerItemId(EmbySeason season) => season.ItemId;
    protected override string MediaServerItemId(EmbyEpisode episode) => episode.ItemId;

    protected override string MediaServerEtag(EmbyShow show) => show.Etag;
    protected override string MediaServerEtag(EmbySeason season) => season.Etag;
    protected override string MediaServerEtag(EmbyEpisode episode) => episode.Etag;

    protected override IAsyncEnumerable<Tuple<EmbySeason, int>> GetSeasonLibraryItems(
        EmbyLibrary library,
        EmbyConnectionParameters connectionParameters,
        EmbyShow show) =>
        _embyApiClient.GetSeasonLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
            show.ItemId);

    protected override IAsyncEnumerable<Tuple<EmbyEpisode, int>> GetEpisodeLibraryItems(
        EmbyLibrary library,
        EmbyConnectionParameters connectionParameters,
        EmbyShow show,
        EmbySeason season) =>
        _embyApiClient.GetEpisodeLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
            show.ItemId,
            season.ItemId);

    protected override Task<Option<ShowMetadata>> GetFullMetadata(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyShow> result,
        EmbyShow incoming,
        bool deepScan) =>
        Task.FromResult(Option<ShowMetadata>.None);

    protected override Task<Option<SeasonMetadata>> GetFullMetadata(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbySeason> result,
        EmbySeason incoming,
        bool deepScan) =>
        Task.FromResult(Option<SeasonMetadata>.None);

    protected override Task<Option<EpisodeMetadata>> GetFullMetadata(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyEpisode> result,
        EmbyEpisode incoming,
        bool deepScan) =>
        Task.FromResult(Option<EpisodeMetadata>.None);

    protected override Task<Option<Tuple<EpisodeMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyEpisode> result,
        EmbyEpisode incoming) => Task.FromResult(Option<Tuple<EpisodeMetadata, MediaVersion>>.None);

    protected override async Task<Option<MediaVersion>> GetMediaServerStatistics(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyEpisode> result,
        EmbyEpisode incoming)
    {
        _logger.LogDebug("Refreshing {Attribute} for {Path}", "Emby Statistics", result.LocalPath);

        Either<BaseError, MediaVersion> maybeVersion =
            await _embyApiClient.GetPlaybackInfo(
                connectionParameters.Address,
                connectionParameters.ApiKey,
                library,
                incoming.ItemId);

        foreach (BaseError error in maybeVersion.LeftToSeq())
        {
            _logger.LogWarning("Failed to get episode statistics from Emby: {Error}", error.ToString());
        }

        // chapters are pulled with metadata, not with statistics, but we need to save them here
        foreach (MediaVersion version in maybeVersion.RightToSeq())
        {
            version.Chapters = result.Item.GetHeadVersion().Chapters;
        }

        return maybeVersion.ToOption();
    }


    protected override Task<Either<BaseError, MediaItemScanResult<EmbyShow>>> UpdateMetadata(
        MediaItemScanResult<EmbyShow> result,
        ShowMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbyShow>>>(result);

    protected override Task<Either<BaseError, MediaItemScanResult<EmbySeason>>> UpdateMetadata(
        MediaItemScanResult<EmbySeason> result,
        SeasonMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbySeason>>>(result);

    protected override Task<Either<BaseError, MediaItemScanResult<EmbyEpisode>>> UpdateMetadata(
        MediaItemScanResult<EmbyEpisode> result,
        EpisodeMetadata fullMetadata,
        CancellationToken cancellationToken) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbyEpisode>>>(result);

    private async Task<Either<BaseError, Unit>> ScanSingleShowInternal(
        IEmbyTelevisionRepository televisionRepository,
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        EmbyShow targetShow,
        Func<EmbyEpisode, string> getLocalPath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            async IAsyncEnumerable<Tuple<EmbyShow, int>> GetSingleShow()
            {
                yield return new Tuple<EmbyShow, int>(targetShow, 1);
                await Task.CompletedTask;
            }

            return await ScanLibraryWithoutCleanup(
                televisionRepository,
                connectionParameters,
                library,
                getLocalPath,
                GetSingleShow(),
                deepScan,
                cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
    }
}
