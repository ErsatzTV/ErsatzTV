using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class MovieFolderScannerTests
    {
        private static readonly string BadFakeRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Movies-That-Dont-Exist"
            : @"/movies-that-dont-exist";

        private static readonly string FakeRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Movies"
            : "/movies";

        private static readonly string FFprobePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\bin\ffprobe.exe"
            : "/bin/ffprobe";

        [TestFixture]
        public class ScanFolder
        {
            [SetUp]
            public void SetUp()
            {
                _movieRepository = new Mock<IMovieRepository>();
                _movieRepository.Setup(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()))
                    .Returns(
                        (LibraryPath _, string path) =>
                            Right<BaseError, Movie>(new Movie { Path = path }).AsTask());

                _localStatisticsProvider = new Mock<ILocalStatisticsProvider>();
                _localMetadataProvider = new Mock<ILocalMetadataProvider>();

                _imageCache = new Mock<IImageCache>();
                _imageCache.Setup(
                        x => x.ResizeAndSaveImage(FakeLocalFileSystem.TestBytes, It.IsAny<int?>(), It.IsAny<int?>()))
                    .Returns(Right<BaseError, string>("poster").AsTask());
            }

            private Mock<IMovieRepository> _movieRepository;
            private Mock<ILocalStatisticsProvider> _localStatisticsProvider;
            private Mock<ILocalMetadataProvider> _localMetadataProvider;
            private Mock<IImageCache> _imageCache;

            [Test]
            public async Task Missing_Folder()
            {
                MovieFolderScanner service = GetService(
                    new FakeFileEntry(Path.Combine(FakeRoot, Path.Combine("Movie (2020)", "Movie (2020).mkv")))
                );
                var libraryPath = new LibraryPath { Path = BadFakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsLeft.Should().BeTrue();
                result.IfLeft(error => error.Should().BeOfType<MediaSourceInaccessible>());
            }

            [Test]
            public async Task NewMovie_Statistics_And_FallbackMetadata(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);
            }

            [Test]
            public async Task NewMovie_Statistics_And_SidecarMetadata_MovieNameNfo(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                string metadataPath = Path.ChangeExtension(moviePath, "nfo");

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(metadataPath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshSidecarMetadata(It.Is<MediaItem>(i => i.Path == moviePath), metadataPath),
                    Times.Once);
            }

            [Test]
            public async Task NewMovie_Statistics_And_SidecarMetadata_MovieNfo(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                string metadataPath = Path.Combine(Path.GetDirectoryName(moviePath) ?? string.Empty, "movie.nfo");

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(metadataPath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshSidecarMetadata(It.Is<MediaItem>(i => i.Path == moviePath), metadataPath),
                    Times.Once);
            }

            [Test]
            public async Task NewMovie_Statistics_And_FallbackMetadata_And_Poster(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension,
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.ImageFileExtensions))]
                string imageExtension)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                string posterPath = Path.Combine(
                    Path.GetDirectoryName(moviePath) ?? string.Empty,
                    $"poster.{imageExtension}");

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(posterPath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _imageCache.Verify(
                    x => x.ResizeAndSaveImage(FakeLocalFileSystem.TestBytes, It.IsAny<int?>(), It.IsAny<int?>()),
                    Times.Once);
            }

            [Test]
            public async Task NewMovie_Statistics_And_FallbackMetadata_And_MovieNamePoster(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension,
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.ImageFileExtensions))]
                string imageExtension)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                string posterPath = Path.Combine(
                    Path.GetDirectoryName(moviePath) ?? string.Empty,
                    $"Movie (2020)-poster.{imageExtension}");

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(posterPath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _imageCache.Verify(
                    x => x.ResizeAndSaveImage(FakeLocalFileSystem.TestBytes, It.IsAny<int?>(), It.IsAny<int?>()),
                    Times.Once);
            }

            [Test]
            public async Task Should_Ignore_Extra_Files(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension,
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.ExtraFiles))]
                string extraFile)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(
                        Path.Combine(
                            Path.GetDirectoryName(moviePath) ?? string.Empty,
                            $"Movie (2020)-{extraFile}{videoExtension}"))
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);
            }

            [Test]
            public async Task Should_Ignore_Extra_Folders(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension,
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.ExtraDirectories))]
                string extraFolder)
            {
                string moviePath = Path.Combine(
                    FakeRoot,
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath),
                    new FakeFileEntry(
                        Path.Combine(
                            Path.GetDirectoryName(moviePath) ?? string.Empty,
                            Path.Combine(extraFolder, $"Movie (2020){videoExtension}")))
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);
            }

            [Test]
            public async Task Should_Work_With_Nested_Folders(
                [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
                string videoExtension)
            {
                string moviePath = Path.Combine(
                    Path.Combine(FakeRoot, "L-P"),
                    Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

                MovieFolderScanner service = GetService(
                    new FakeFileEntry(moviePath)
                );
                var libraryPath = new LibraryPath { Id = 1, Path = FakeRoot };

                Either<BaseError, Unit> result = await service.ScanFolder(libraryPath, FFprobePath);

                result.IsRight.Should().BeTrue();

                _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
                _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

                _localStatisticsProvider.Verify(
                    x => x.RefreshStatistics(FFprobePath, It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);

                _localMetadataProvider.Verify(
                    x => x.RefreshFallbackMetadata(It.Is<MediaItem>(i => i.Path == moviePath)),
                    Times.Once);
            }

            private MovieFolderScanner GetService(params FakeFileEntry[] files) =>
                // var mockImageCache = new Mock<IImageCache>();
                // mockImageCache.Setup(i => i.ResizeAndSaveImage(It.IsAny<byte[]>(), It.IsAny<int?>(), It.IsAny<int?>()))
                //     .Returns(Right<BaseError, string>("image").AsTask());
                new(
                    new FakeLocalFileSystem(new List<FakeFileEntry>(files)),
                    _movieRepository.Object,
                    _localStatisticsProvider.Object,
                    _localMetadataProvider.Object,
                    _imageCache.Object,
                    new Mock<ILogger<MovieFolderScanner>>().Object
                );
        }
    }
}
