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

public class JellyfinMovieLibraryScanner :
    MediaServerMovieLibraryScanner<JellyfinConnectionParameters, JellyfinLibrary, JellyfinMovie, JellyfinItemEtag>,
    IJellyfinMovieLibraryScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IJellyfinMovieRepository _jellyfinMovieRepository;
    private readonly ILogger<JellyfinMovieLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinPathReplacementService _pathReplacementService;

    public JellyfinMovieLibraryScanner(
        IJellyfinApiClient jellyfinApiClient,
        IMediator mediator,
        IJellyfinMovieRepository jellyfinMovieRepository,
        IJellyfinPathReplacementService pathReplacementService,
        IMediaSourceRepository mediaSourceRepository,
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        ILogger<JellyfinMovieLibraryScanner> logger)
        : base(
            localFileSystem,
            metadataRepository,
            mediator,
            logger)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _jellyfinMovieRepository = jellyfinMovieRepository;
        _pathReplacementService = pathReplacementService;
        _mediaSourceRepository = mediaSourceRepository;
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
            deepScan,
            cancellationToken);
    }

    protected override string MediaServerItemId(JellyfinMovie movie) => movie.ItemId;

    protected override string MediaServerEtag(JellyfinMovie movie) => movie.Etag;

    protected override IAsyncEnumerable<Tuple<JellyfinMovie, int>> GetMovieLibraryItems(
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

    protected override Task<Option<Tuple<MovieMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinMovie> result,
        JellyfinMovie incoming) => Task.FromResult(Option<Tuple<MovieMetadata, MediaVersion>>.None);

    protected override async Task<Option<MediaVersion>> GetMediaServerStatistics(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        MediaItemScanResult<JellyfinMovie> result,
        JellyfinMovie incoming)
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
            _logger.LogWarning("Failed to get movie statistics from Jellyfin: {Error}", error.ToString());
        }

        // chapters are pulled with metadata, not with statistics, but we need to save them here
        foreach (MediaVersion version in maybeVersion.RightToSeq())
        {
            version.Chapters = result.Item.GetHeadVersion().Chapters;
        }

        return maybeVersion.ToOption();
    }

    protected override Task<Either<BaseError, MediaItemScanResult<JellyfinMovie>>> UpdateMetadata(
        MediaItemScanResult<JellyfinMovie> result,
        MovieMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<JellyfinMovie>>>(result);
}
