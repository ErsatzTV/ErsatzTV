using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class MusicVideoFolderScanner : LocalFolderScanner, IMusicVideoFolderScanner
    {
        public MusicVideoFolderScanner(
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            ILogger<MusicVideoFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            imageCache,
            logger)
        {
        }

        public Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            DateTimeOffset lastScan) => Right<BaseError, Unit>(Unit.Default).AsTask();
    }
}
