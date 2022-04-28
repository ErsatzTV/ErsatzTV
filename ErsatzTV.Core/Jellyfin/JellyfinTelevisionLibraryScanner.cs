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

public class JellyfinTelevisionLibraryScanner : MediaServerTelevisionLibraryScanner<JellyfinConnectionParameters,
    JellyfinLibrary,
    JellyfinShow, JellyfinSeason, JellyfinEpisode,
    JellyfinItemEtag>, IJellyfinTelevisionLibraryScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinPathReplacementService _pathReplacementService;
    private readonly IJellyfinTelevisionRepository _televisionRepository;

    public JellyfinTelevisionLibraryScanner(
        IJellyfinApiClient jellyfinApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinTelevisionRepository televisionRepository,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IJellyfinPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMediator mediator,
        ILogger<JellyfinTelevisionLibraryScanner> logger)
        : base(
            localStatisticsProvider,
            localSubtitlesProvider,
            localFileSystem,
            searchRepository,
            searchIndex,
            mediator,
            logger)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _televisionRepository = televisionRepository;
        _pathReplacementService = pathReplacementService;
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

        string GetLocalPath(JellyfinEpisode episode)
        {
            return _pathReplacementService.GetReplacementJellyfinPath(
                pathReplacements,
                episode.GetHeadVersion().MediaFiles.Head().Path,
                false);
        }

        return await ScanLibrary(
            _televisionRepository,
            new JellyfinConnectionParameters(address, apiKey, library.MediaSourceId),
            library,
            GetLocalPath,
            ffmpegPath,
            ffprobePath,
            false,
            cancellationToken);
    }

    protected override Task<Either<BaseError, List<JellyfinShow>>> GetShowLibraryItems(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library) =>
        _jellyfinApiClient.GetShowLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library.MediaSourceId,
            library.ItemId);

    protected override string MediaServerItemId(JellyfinShow show) => show.ItemId;
    protected override string MediaServerItemId(JellyfinSeason season) => season.ItemId;
    protected override string MediaServerItemId(JellyfinEpisode episode) => episode.ItemId;

    protected override string MediaServerEtag(JellyfinShow show) => show.Etag;
    protected override string MediaServerEtag(JellyfinSeason season) => season.Etag;
    protected override string MediaServerEtag(JellyfinEpisode episode) => episode.Etag;

    protected override Task<Either<BaseError, List<JellyfinSeason>>> GetSeasonLibraryItems(
        JellyfinLibrary library,
        JellyfinConnectionParameters connectionParameters,
        JellyfinShow show) =>
        _jellyfinApiClient.GetSeasonLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library.MediaSourceId,
            show.ItemId);

    protected override Task<Either<BaseError, List<JellyfinEpisode>>> GetEpisodeLibraryItems(
        JellyfinLibrary library,
        JellyfinConnectionParameters connectionParameters,
        JellyfinSeason season) =>
        _jellyfinApiClient.GetEpisodeLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library.MediaSourceId,
            season.ItemId);

    protected override Task<Option<ShowMetadata>> GetFullMetadata(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinShow> result,
        JellyfinShow incoming,
        bool deepScan) =>
        Task.FromResult(Option<ShowMetadata>.None);

    protected override Task<Option<SeasonMetadata>> GetFullMetadata(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinSeason> result,
        JellyfinSeason incoming,
        bool deepScan) =>
        Task.FromResult(Option<SeasonMetadata>.None);

    protected override Task<Option<EpisodeMetadata>> GetFullMetadata(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinEpisode> result,
        JellyfinEpisode incoming,
        bool deepScan) =>
        Task.FromResult(Option<EpisodeMetadata>.None);

    protected override Task<Either<BaseError, MediaItemScanResult<JellyfinShow>>> UpdateMetadata(
        MediaItemScanResult<JellyfinShow> result,
        ShowMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<JellyfinShow>>>(result);

    protected override Task<Either<BaseError, MediaItemScanResult<JellyfinSeason>>> UpdateMetadata(
        MediaItemScanResult<JellyfinSeason> result,
        SeasonMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<JellyfinSeason>>>(result);

    protected override Task<Either<BaseError, MediaItemScanResult<JellyfinEpisode>>> UpdateMetadata(
        MediaItemScanResult<JellyfinEpisode> result,
        EpisodeMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<JellyfinEpisode>>>(result);
}
