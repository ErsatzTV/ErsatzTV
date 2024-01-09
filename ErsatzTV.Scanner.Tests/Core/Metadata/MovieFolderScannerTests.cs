using System.Runtime.InteropServices;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using ErsatzTV.Scanner.Core.Metadata;
using ErsatzTV.Scanner.Tests.Core.Fakes;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
            _movieRepository = Substitute.For<IMovieRepository>();
            _movieRepository.GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>())
                .Returns(
                    args =>
                        Right<BaseError, MediaItemScanResult<Movie>>(new FakeMovieWithPath(args.Arg<string>()))
                            .AsTask());
            _movieRepository.FindMoviePaths(Arg.Any<LibraryPath>())
                .Returns(new List<string>().AsEnumerable().AsTask());

            _mediaItemRepository = Substitute.For<IMediaItemRepository>();
            _mediaItemRepository.FlagFileNotFound(Arg.Any<LibraryPath>(), Arg.Any<string>())
                .Returns(new List<int>().AsTask());

            _localStatisticsProvider = Substitute.For<ILocalStatisticsProvider>();
            _localMetadataProvider = Substitute.For<ILocalMetadataProvider>();

            _localStatisticsProvider.RefreshStatistics(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MediaItem>())
                .Returns(Right<BaseError, bool>(true).AsTask());

            // fallback metadata adds metadata to a movie, so we need to replicate that here
            _localMetadataProvider.RefreshFallbackMetadata(Arg.Any<Movie>())
                .Returns(
                    arg =>
                    {
                        ((Movie)arg.Arg<MediaItem>()).MovieMetadata = new List<MovieMetadata> { new() };
                        return Task.FromResult(true);
                    });

            _imageCache = Substitute.For<IImageCache>();
        }

        private IMovieRepository _movieRepository;
        private IMediaItemRepository _mediaItemRepository;
        private ILocalStatisticsProvider _localStatisticsProvider;
        private ILocalMetadataProvider _localMetadataProvider;
        private IImageCache _imageCache;

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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
        }

        [Test]
        public async Task NewMovie_Statistics_And_FallbackMetadata_MixedCase(
            [ValueSource(typeof(LocalFolderScanner), nameof(LocalFolderScanner.VideoFileExtensions))]
            string videoExtension)
        {
            char[] mixedCaseExtension = videoExtension.ToLowerInvariant().ToArray();
            mixedCaseExtension[2] = char.ToUpper(mixedCaseExtension[2]);
            videoExtension = new string(mixedCaseExtension);

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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshSidecarMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath),
                metadataPath);
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshSidecarMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath),
                metadataPath);
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _imageCache.Received(1).CopyArtworkToCache(posterPath, ArtworkKind.Poster);
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _imageCache.Received(1).CopyArtworkToCache(posterPath, ArtworkKind.Poster);
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _imageCache.Received(1).CopyArtworkToCache(posterPath, ArtworkKind.Poster);
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
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

            await _movieRepository.Received(1).GetOrAdd(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _movieRepository.Received(1).GetOrAdd(libraryPath, moviePath);

            await _localStatisticsProvider.Received(1).RefreshStatistics(
                FFmpegPath,
                FFprobePath,
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));

            await _localMetadataProvider.Received(1).RefreshFallbackMetadata(
                Arg.Is<Movie>(i => i.MediaVersions.Head().MediaFiles.Head().Path == moviePath));
        }

        [Test]
        public async Task RenamedMovie_Should_Delete_Old_Movie()
        {
            // TODO: handle this case more elegantly
            // ideally, detect that the movie was renamed and still delete the old one (or update the path?)

            string movieFolder = Path.Combine(FakeRoot, "Movie (2020)");
            string oldMoviePath = Path.Combine(movieFolder, "Movie (2020).avi");

            _movieRepository.FindMoviePaths(Arg.Any<LibraryPath>())
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

            await _mediaItemRepository.Received(1).FlagFileNotFound(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _mediaItemRepository.Received(1).FlagFileNotFound(libraryPath, oldMoviePath);
        }

        [Test]
        public async Task DeletedMovieAndFolder_Should_Flag_File_Not_Found()
        {
            string movieFolder = Path.Combine(FakeRoot, "Movie (2020)");
            string oldMoviePath = Path.Combine(movieFolder, "Movie (2020).avi");

            _movieRepository.FindMoviePaths(Arg.Any<LibraryPath>())
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

            await _mediaItemRepository.Received(1).FlagFileNotFound(Arg.Any<LibraryPath>(), Arg.Any<string>());
            await _mediaItemRepository.Received(1).FlagFileNotFound(libraryPath, oldMoviePath);
        }

        private MovieFolderScanner GetService(params FakeFileEntry[] files) =>
            new(
                new FakeLocalFileSystem(new List<FakeFileEntry>(files)),
                _movieRepository,
                _localStatisticsProvider,
                Substitute.For<ILocalSubtitlesProvider>(),
                _localMetadataProvider,
                Substitute.For<IMetadataRepository>(),
                _imageCache,
                Substitute.For<ILibraryRepository>(),
                _mediaItemRepository,
                Substitute.For<IMediator>(),
                Substitute.For<IFFmpegPngService>(),
                Substitute.For<ITempFilePool>(),
                Substitute.For<IClient>(),
                Substitute.For<ILogger<MovieFolderScanner>>()
            );

        private MovieFolderScanner GetService(params FakeFolderEntry[] folders) =>
            new(
                new FakeLocalFileSystem(new List<FakeFileEntry>(), new List<FakeFolderEntry>(folders)),
                _movieRepository,
                _localStatisticsProvider,
                Substitute.For<ILocalSubtitlesProvider>(),
                _localMetadataProvider,
                Substitute.For<IMetadataRepository>(),
                _imageCache,
                Substitute.For<ILibraryRepository>(),
                _mediaItemRepository,
                Substitute.For<IMediator>(),
                Substitute.For<IFFmpegPngService>(),
                Substitute.For<ITempFilePool>(),
                Substitute.For<IClient>(),
                Substitute.For<ILogger<MovieFolderScanner>>()
            );
    }
}
