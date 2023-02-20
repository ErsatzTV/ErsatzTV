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

public class EmbyTelevisionLibraryScanner : MediaServerTelevisionLibraryScanner<EmbyConnectionParameters, EmbyLibrary,
    EmbyShow, EmbySeason, EmbyEpisode,
    EmbyItemEtag>, IEmbyTelevisionLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyPathReplacementService _pathReplacementService;
    private readonly ILogger<EmbyTelevisionLibraryScanner> _logger;
    private readonly IEmbyTelevisionRepository _televisionRepository;

    public EmbyTelevisionLibraryScanner(
        IEmbyApiClient embyApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyTelevisionRepository televisionRepository,
        IEmbyPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMediator mediator,
        ILogger<EmbyTelevisionLibraryScanner> logger)
        : base(
            localStatisticsProvider,
            localSubtitlesProvider,
            localFileSystem,
            metadataRepository,
            mediator,
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
        string ffmpegPath,
        string ffprobePath,
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
            ffmpegPath,
            ffprobePath,
            deepScan,
            cancellationToken);
    }

    protected override Task<Either<BaseError, int>> CountShowLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library)
        => _embyApiClient.GetLibraryItemCount(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library.ItemId,
            EmbyItemType.Show);

    protected override IAsyncEnumerable<EmbyShow> GetShowLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library) =>
        _embyApiClient.GetShowLibraryItems(connectionParameters.Address, connectionParameters.ApiKey, library);

    protected override string MediaServerItemId(EmbyShow show) => show.ItemId;
    protected override string MediaServerItemId(EmbySeason season) => season.ItemId;
    protected override string MediaServerItemId(EmbyEpisode episode) => episode.ItemId;

    protected override string MediaServerEtag(EmbyShow show) => show.Etag;
    protected override string MediaServerEtag(EmbySeason season) => season.Etag;
    protected override string MediaServerEtag(EmbyEpisode episode) => episode.Etag;

    protected override Task<Either<BaseError, int>> CountSeasonLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        EmbyShow show) =>
        _embyApiClient.GetLibraryItemCount(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            show.ItemId,
            EmbyItemType.Season);

    protected override IAsyncEnumerable<EmbySeason> GetSeasonLibraryItems(
        EmbyLibrary library,
        EmbyConnectionParameters connectionParameters,
        EmbyShow show) =>
        _embyApiClient.GetSeasonLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
            show.ItemId);

    protected override Task<Either<BaseError, int>> CountEpisodeLibraryItems(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        EmbySeason season) =>
        _embyApiClient.GetLibraryItemCount(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            season.ItemId,
            EmbyItemType.Episode);

    protected override IAsyncEnumerable<EmbyEpisode> GetEpisodeLibraryItems(
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
        EpisodeMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbyEpisode>>>(result);
}
