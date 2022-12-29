using System.Runtime.InteropServices;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Tests.Core.Fakes;
using ErsatzTV.Scanner.Core.Metadata;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Scanner.Tests.Core.Metadata;

[TestFixture]
public class MovieFolderScannerTests
{
    private static readonly string BadFakeRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\Movies-That-Dont-Exist"
        : @"/movies-that-dont-exist";

    private static readonly string FakeRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\Movies"
        : "/movies";

    private static readonly string FFmpegPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\bin\ffmpeg.exe"
        : "/bin/ffmpeg";

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
                        Right<BaseError, MediaItemScanResult<Movie>>(new FakeMovieWithPath(path)).AsTask());
            _movieRepository.Setup(x => x.FindMoviePaths(It.IsAny<LibraryPath>()))
                .Returns(new List<string>().AsEnumerable().AsTask());

            _mediaItemRepository = new Mock<IMediaItemRepository>();
            _mediaItemRepository.Setup(x => x.FlagFileNotFound(It.IsAny<LibraryPath>(), It.IsAny<string>()))
                .Returns(new List<int>().AsTask());

            _localStatisticsProvider = new Mock<ILocalStatisticsProvider>();
            _localMetadataProvider = new Mock<ILocalMetadataProvider>();

            _localStatisticsProvider.Setup(
                    x => x.RefreshStatistics(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaItem>()))
                .Returns<string, string, MediaItem>((_, _, _) => Right<BaseError, bool>(true).AsTask());

            // fallback metadata adds metadata to a movie, so we need to replicate that here
            _localMetadataProvider.Setup(x => x.RefreshFallbackMetadata(It.IsAny<Movie>()))
                .Returns(
                    (MediaItem mediaItem) =>
                    {
                        ((Movie)mediaItem).MovieMetadata = new List<MovieMetadata> { new() };
                        return Task.FromResult(true);
                    });

            _imageCache = new Mock<IImageCache>();
        }

        private Mock<IMovieRepository> _movieRepository;
        private Mock<IMediaItemRepository> _mediaItemRepository;
        private Mock<ILocalStatisticsProvider> _localStatisticsProvider;
        private Mock<ILocalMetadataProvider> _localMetadataProvider;
        private Mock<IImageCache> _imageCache;

        [Test]
        public async Task NewMovie_Statistics_And_FallbackMetadata(
            [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
            string videoExtension)
        {
            string moviePath = Path.Combine(
                FakeRoot,
                Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

            MovieFolderScanner service = GetService(
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(metadataPath)
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshSidecarMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath),
                    metadataPath),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(metadataPath)
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshSidecarMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath),
                    metadataPath),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(posterPath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _imageCache.Verify(
                x => x.CopyArtworkToCache(posterPath, ArtworkKind.Poster),
                Times.Once);
        }

        [Test]
        public async Task NewMovie_Statistics_And_FallbackMetadata_And_FolderPoster(
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
                $"folder.{imageExtension}");

            MovieFolderScanner service = GetService(
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(posterPath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _imageCache.Verify(
                x => x.CopyArtworkToCache(posterPath, ArtworkKind.Poster),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(posterPath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _imageCache.Verify(
                x => x.CopyArtworkToCache(posterPath, ArtworkKind.Poster),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(
                    Path.Combine(
                        Path.GetDirectoryName(moviePath) ?? string.Empty,
                        $"Movie (2020)-{extraFile}{videoExtension}"))
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);
        }

        [Test]
        public async Task Should_Ignore_Dot_Underscore_Files(
            [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
            string videoExtension)
        {
            string moviePath = Path.Combine(
                FakeRoot,
                Path.Combine("Movie (2020)", $"Movie (2020){videoExtension}"));

            MovieFolderScanner service = GetService(
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(
                    Path.Combine(
                        Path.GetDirectoryName(moviePath) ?? string.Empty,
                        $"._Movie (2020){videoExtension}"))
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now },
                new FakeFileEntry(
                    Path.Combine(
                        Path.GetDirectoryName(moviePath) ?? string.Empty,
                        Path.Combine(extraFolder, $"Movie (2020){videoExtension}")))
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
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
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _movieRepository.Verify(x => x.GetOrAdd(It.IsAny<LibraryPath>(), It.IsAny<string>()), Times.Once);
            _movieRepository.Verify(x => x.GetOrAdd(libraryPath, moviePath), Times.Once);

            _localStatisticsProvider.Verify(
                x => x.RefreshStatistics(
                    FFmpegPath,
                    FFprobePath,
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);

            _localMetadataProvider.Verify(
                x => x.RefreshFallbackMetadata(
                    It.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath)),
                Times.Once);
        }

        [Test]
        public async Task RenamedMovie_Should_Delete_Old_Movie()
        {
            // TODO: handle this case more elegantly
            // ideally, detect that the movie was renamed and still delete the old one (or update the path?)

            string movieFolder = Path.Combine(FakeRoot, "Movie (2020)");
            string oldMoviePath = Path.Combine(movieFolder, "Movie (2020).avi");

            _movieRepository.Setup(x => x.FindMoviePaths(It.IsAny<LibraryPath>()))
                .Returns(new List<string> { oldMoviePath }.AsEnumerable().AsTask());

            string moviePath = Path.Combine(movieFolder, "Movie (2020).mkv");

            MovieFolderScanner service = GetService(
                new FakeFileEntry(moviePath) { LastWriteTime = DateTime.Now }
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _mediaItemRepository.Verify(
                x => x.FlagFileNotFound(It.IsAny<LibraryPath>(), It.IsAny<string>()),
                Times.Once);
            _mediaItemRepository.Verify(x => x.FlagFileNotFound(libraryPath, oldMoviePath), Times.Once);
        }

        [Test]
        public async Task DeletedMovieAndFolder_Should_Flag_File_Not_Found()
        {
            string movieFolder = Path.Combine(FakeRoot, "Movie (2020)");
            string oldMoviePath = Path.Combine(movieFolder, "Movie (2020).avi");

            _movieRepository.Setup(x => x.FindMoviePaths(It.IsAny<LibraryPath>()))
                .Returns(new List<string> { oldMoviePath }.AsEnumerable().AsTask());

            MovieFolderScanner service = GetService(
                new FakeFolderEntry(FakeRoot)
            );
            var libraryPath = new LibraryPath
                { Id = 1, Path = FakeRoot, LibraryFolders = new List<LibraryFolder>() };

            Either<BaseError, Unit> result = await service.ScanFolder(
                libraryPath,
                FFmpegPath,
                FFprobePath,
                0,
                1,
                CancellationToken.None);

            result.IsRight.Should().BeTrue();

            _mediaItemRepository.Verify(
                x => x.FlagFileNotFound(It.IsAny<LibraryPath>(), It.IsAny<string>()),
                Times.Once);
            _mediaItemRepository.Verify(x => x.FlagFileNotFound(libraryPath, oldMoviePath), Times.Once);
        }

        private MovieFolderScanner GetService(params FakeFileEntry[] files) =>
            new(
                new FakeLocalFileSystem(new List<FakeFileEntry>(files)),
                _movieRepository.Object,
                _localStatisticsProvider.Object,
                new Mock<ILocalSubtitlesProvider>().Object,
                _localMetadataProvider.Object,
                new Mock<IMetadataRepository>().Object,
                _imageCache.Object,
                new Mock<ISearchIndex>().Object,
                new Mock<ICachingSearchRepository>().Object,
                new Mock<IFallbackMetadataProvider>().Object,
                new Mock<ILibraryRepository>().Object,
                _mediaItemRepository.Object,
                new Mock<IMediator>().Object,
                new Mock<IFFmpegPngService>().Object,
                new Mock<ITempFilePool>().Object,
                new Mock<IClient>().Object,
                new Mock<ILogger<MovieFolderScanner>>().Object
            );

        private MovieFolderScanner GetService(params FakeFolderEntry[] folders) =>
            new(
                new FakeLocalFileSystem(new List<FakeFileEntry>(), new List<FakeFolderEntry>(folders)),
                _movieRepository.Object,
                _localStatisticsProvider.Object,
                new Mock<ILocalSubtitlesProvider>().Object,
                _localMetadataProvider.Object,
                new Mock<IMetadataRepository>().Object,
                _imageCache.Object,
                new Mock<ISearchIndex>().Object,
                new Mock<ICachingSearchRepository>().Object,
                new Mock<IFallbackMetadataProvider>().Object,
                new Mock<ILibraryRepository>().Object,
                _mediaItemRepository.Object,
                new Mock<IMediator>().Object,
                new Mock<IFFmpegPngService>().Object,
                new Mock<ITempFilePool>().Object,
                new Mock<IClient>().Object,
                new Mock<ILogger<MovieFolderScanner>>().Object
            );
    }
}
