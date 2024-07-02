﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Emby;

public class EmbyMovieLibraryScanner :
    MediaServerMovieLibraryScanner<EmbyConnectionParameters, EmbyLibrary, EmbyMovie, EmbyItemEtag>,
    IEmbyMovieLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbyMovieRepository _embyMovieRepository;
    private readonly ILogger<EmbyMovieLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyPathReplacementService _pathReplacementService;

    public EmbyMovieLibraryScanner(
        IEmbyApiClient embyApiClient,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyMovieRepository embyMovieRepository,
        IEmbyPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        ILogger<EmbyMovieLibraryScanner> logger)
        : base(
            localFileSystem,
            metadataRepository,
            mediator,
            logger)
    {
        _embyApiClient = embyApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _embyMovieRepository = embyMovieRepository;
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
            deepScan,
            cancellationToken);
    }

    protected override string MediaServerItemId(EmbyMovie movie) => movie.ItemId;
    protected override string MediaServerEtag(EmbyMovie movie) => movie.Etag;

    protected override IAsyncEnumerable<Tuple<EmbyMovie, int>> GetMovieLibraryItems(
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

    protected override Task<Option<Tuple<MovieMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyMovie> result,
        EmbyMovie incoming) => Task.FromResult(Option<Tuple<MovieMetadata, MediaVersion>>.None);

    protected override async Task<Option<MediaVersion>> GetMediaServerStatistics(
        EmbyConnectionParameters connectionParameters,
        EmbyLibrary library,
        MediaItemScanResult<EmbyMovie> result,
        EmbyMovie incoming)
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
            _logger.LogWarning("Failed to get movie statistics from Emby: {Error}", error.ToString());
        }

        // chapters are pulled with metadata, not with statistics, but we need to save them here
        foreach (MediaVersion version in maybeVersion.RightToSeq())
        {
            version.Chapters = result.Item.GetHeadVersion().Chapters;
        }

        return maybeVersion.ToOption();
    }

    protected override Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> UpdateMetadata(
        MediaItemScanResult<EmbyMovie> result,
        MovieMetadata fullMetadata) =>
        Task.FromResult<Either<BaseError, MediaItemScanResult<EmbyMovie>>>(result);
}
