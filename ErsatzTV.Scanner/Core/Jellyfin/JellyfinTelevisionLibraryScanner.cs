﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Jellyfin;

public class JellyfinTelevisionLibraryScanner : MediaServerTelevisionLibraryScanner<JellyfinConnectionParameters,
    JellyfinLibrary,
    JellyfinShow, JellyfinSeason, JellyfinEpisode,
    JellyfinItemEtag>, IJellyfinTelevisionLibraryScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly ILogger<JellyfinTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinPathReplacementService _pathReplacementService;
    private readonly IJellyfinTelevisionRepository _televisionRepository;

    public JellyfinTelevisionLibraryScanner(
        IJellyfinApiClient jellyfinApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinTelevisionRepository televisionRepository,
        IJellyfinPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        IMediator mediator,
        ILogger<JellyfinTelevisionLibraryScanner> logger)
        : base(
            localFileSystem,
            metadataRepository,
            mediator,
            logger)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _televisionRepository = televisionRepository;
        _pathReplacementService = pathReplacementService;
        _logger = logger;
    }

    protected override bool ServerSupportsRemoteStreaming => true;

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        bool deepScan,
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
            deepScan,
            cancellationToken);
    }

    protected override IAsyncEnumerable<Tuple<JellyfinShow, int>> GetShowLibraryItems(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library) =>
        _jellyfinApiClient.GetShowLibraryItems(connectionParameters.Address, connectionParameters.ApiKey, library);

    protected override string MediaServerItemId(JellyfinShow show) => show.ItemId;
    protected override string MediaServerItemId(JellyfinSeason season) => season.ItemId;
    protected override string MediaServerItemId(JellyfinEpisode episode) => episode.ItemId;

    protected override string MediaServerEtag(JellyfinShow show) => show.Etag;
    protected override string MediaServerEtag(JellyfinSeason season) => season.Etag;
    protected override string MediaServerEtag(JellyfinEpisode episode) => episode.Etag;

    protected override IAsyncEnumerable<Tuple<JellyfinSeason, int>> GetSeasonLibraryItems(
        JellyfinLibrary library,
        JellyfinConnectionParameters connectionParameters,
        JellyfinShow show) =>
        _jellyfinApiClient.GetSeasonLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
            show.ItemId);

    protected override IAsyncEnumerable<Tuple<JellyfinEpisode, int>> GetEpisodeLibraryItems(
        JellyfinLibrary library,
        JellyfinConnectionParameters connectionParameters,
        JellyfinShow show,
        JellyfinSeason season) =>
        _jellyfinApiClient.GetEpisodeLibraryItems(
            connectionParameters.Address,
            connectionParameters.ApiKey,
            library,
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

    protected override Task<Option<Tuple<EpisodeMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinEpisode> result,
        JellyfinEpisode incoming) => Task.FromResult(Option<Tuple<EpisodeMetadata, MediaVersion>>.None);

    protected override async Task<Option<MediaVersion>> GetMediaServerStatistics(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinEpisode> result,
        JellyfinEpisode incoming)
    {
        _logger.LogDebug("Refreshing {Attribute} for {Path}", "Jellyfin Statistics", result.LocalPath);

        Either<BaseError, MediaVersion> maybeVersion =
            await _jellyfinApiClient.GetPlaybackInfo(
                connectionParameters.Address,
                connectionParameters.ApiKey,
                library,
                incoming.ItemId);

        foreach (BaseError error in maybeVersion.LeftToSeq())
        {
            _logger.LogWarning("Failed to get episode statistics from Jellyfin: {Error}", error.ToString());
        }

        // chapters are pulled with metadata, not with statistics, but we need to save them here
        foreach (MediaVersion version in maybeVersion.RightToSeq())
        {
            version.Chapters = result.Item.GetHeadVersion().Chapters;
        }

        return maybeVersion.ToOption();
    }

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
