using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Search.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class ScanLocalLibraryHandler : IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>,
        IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IEntityLocker _entityLocker;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<ScanLocalLibraryHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMovieFolderScanner _movieFolderScanner;
        private readonly IMusicVideoFolderScanner _musicVideoFolderScanner;
        private readonly ChannelWriter<ISearchBackgroundServiceRequest> _searchChannel;
        private readonly ITelevisionFolderScanner _televisionFolderScanner;

        public ScanLocalLibraryHandler(
            ILibraryRepository libraryRepository,
            IConfigElementRepository configElementRepository,
            IMovieFolderScanner movieFolderScanner,
            ITelevisionFolderScanner televisionFolderScanner,
            IMusicVideoFolderScanner musicVideoFolderScanner,
            IEntityLocker entityLocker,
            IMediator mediator,
            ILogger<ScanLocalLibraryHandler> logger,
            ChannelWriter<ISearchBackgroundServiceRequest> searchChannel)
        {
            _libraryRepository = libraryRepository;
            _configElementRepository = configElementRepository;
            _movieFolderScanner = movieFolderScanner;
            _televisionFolderScanner = televisionFolderScanner;
            _musicVideoFolderScanner = musicVideoFolderScanner;
            _entityLocker = entityLocker;
            _mediator = mediator;
            _logger = logger;
            _searchChannel = searchChannel;
        }

        public Task<Either<BaseError, string>> Handle(
            ForceScanLocalLibrary request,
            CancellationToken cancellationToken) => Handle(request);

        public Task<Either<BaseError, string>> Handle(
            ScanLocalLibraryIfNeeded request,
            CancellationToken cancellationToken) => Handle(request);

        private Task<Either<BaseError, string>>
            Handle(IScanLocalLibrary request) =>
            Validate(request)
                .MapT(parameters => PerformScan(parameters).Map(_ => parameters.LocalLibrary.Name))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> PerformScan(RequestParameters parameters)
        {
            (LocalLibrary localLibrary, string ffprobePath, bool forceScan) = parameters;

            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < localLibrary.Paths.Count; i++)
            {
                LibraryPath libraryPath = localLibrary.Paths[i];

                decimal progressMin = (decimal) i / localLibrary.Paths.Count;
                decimal progressMax = (decimal) (i + 1) / localLibrary.Paths.Count;

                var lastScan = new DateTimeOffset(libraryPath.LastScan ?? DateTime.MinValue, TimeSpan.Zero);
                if (forceScan || lastScan < DateTimeOffset.Now - TimeSpan.FromHours(6))
                {
                    switch (localLibrary.MediaKind)
                    {
                        case LibraryMediaKind.Movies:
                            await _movieFolderScanner.ScanFolder(
                                libraryPath,
                                ffprobePath,
                                lastScan,
                                progressMin,
                                progressMax,
                                AddToSearchIndex,
                                RemoveFromSearchIndex);
                            break;
                        case LibraryMediaKind.Shows:
                            await _televisionFolderScanner.ScanFolder(
                                libraryPath,
                                ffprobePath,
                                lastScan,
                                progressMin,
                                progressMax,
                                AddToSearchIndex,
                                RemoveFromSearchIndex);
                            break;
                        case LibraryMediaKind.MusicVideos:
                            await _musicVideoFolderScanner.ScanFolder(
                                libraryPath,
                                ffprobePath,
                                lastScan,
                                progressMin,
                                progressMax,
                                AddToSearchIndex,
                                RemoveFromSearchIndex);
                            break;
                    }

                    libraryPath.LastScan = DateTime.UtcNow;
                    await _libraryRepository.UpdateLastScan(libraryPath);
                }

                await _mediator.Publish(new LibraryScanProgress(libraryPath.LibraryId, progressMax));
            }

            sw.Stop();
            _logger.LogDebug(
                "Scan of library {Name} completed in {Duration}",
                localLibrary.Name,
                TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));

            await _mediator.Publish(new LibraryScanProgress(localLibrary.Id, 0));

            _entityLocker.UnlockLibrary(localLibrary.Id);
            return Unit.Default;
        }

        private ValueTask AddToSearchIndex(List<MediaItem> mediaItems) =>
            _searchChannel.WriteAsync(new AddItemsToSearchIndex(mediaItems));

        private ValueTask RemoveFromSearchIndex(List<int> mediaItemIds) =>
            _searchChannel.WriteAsync(new RemoveItemsFromSearchIndex(mediaItemIds));

        private async Task<Validation<BaseError, RequestParameters>> Validate(IScanLocalLibrary request) =>
            (await LocalLibraryMustExist(request), await ValidateFFprobePath())
            .Apply(
                (library, ffprobePath) => new RequestParameters(
                    library,
                    ffprobePath,
                    request.ForceScan));

        private Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(
            IScanLocalLibrary request) =>
            _libraryRepository.Get(request.LibraryId)
                .Map(maybeLibrary => maybeLibrary.Map(ms => ms as LocalLibrary))
                .Map(v => v.ToValidation<BaseError>($"Local library {request.LibraryId} does not exist."));

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private record RequestParameters(LocalLibrary LocalLibrary, string FFprobePath, bool ForceScan);
    }
}
