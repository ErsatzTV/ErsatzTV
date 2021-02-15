using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class LocalMediaSourcePlannerTests
    {
        private static readonly List<string> VideoFileExtensions = new()
        {
            "mpg", "mp2", "mpeg", "mpe", "mpv", "ogg", "mp4",
            "m4p", "m4v", "avi", "wmv", "mov", "mkv", "ts"
        };

        private static IEnumerable<FakeFileSystemEntry> OldEntriesFor(params string[] fileNames) =>
            fileNames.Map(f => new FakeFileSystemEntry(f, DateTime.MinValue));

        private static IEnumerable<FakeFileSystemEntry> NewEntriesFor(params string[] fileNames) =>
            fileNames.Map(f => new FakeFileSystemEntry(f, DateTime.MaxValue));

        private static LocalMediaSourcePlanner ScannerForOldFiles(params string[] fileNames)
            => new(new FakeLocalFileSystem(OldEntriesFor(fileNames)));

        private static LocalMediaSourcePlanner ScannerForNewFiles(params string[] fileNames)
            => new(new FakeLocalFileSystem(NewEntriesFor(fileNames)));

        private static LocalMediaSourcePlanner ScannerFor(IEnumerable<FakeFileSystemEntry> entries)
            => new(new FakeLocalFileSystem(entries));

        private static readonly string FakeRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "C:\\"
            : "/";

        private static string MovieNameWithExtension(string extension, int year = 2021) => Path.Combine(
            FakeRoot,
            Path.Combine(
                "movies",
                Path.Combine($"test ({year})"),
                Path.Combine($"test ({year}).{extension}")));

        private static string MovieNfoName(string nfoFileName) =>
            Path.Combine(FakeRoot, Path.Combine("movies", Path.Combine("test (2021)"), nfoFileName));

        private static string MoviePosterNameWithExtension(string basePosterName, string extension) => Path.Combine(
            FakeRoot,
            Path.Combine(
                "movies",
                Path.Combine("test (2021)"),
                $"{basePosterName}poster.{extension}"));

        private static string EpisodeNameWithExtension(string extension, int episodeNumber = 3) => Path.Combine(
            FakeRoot,
            Path.Combine(
                "tv",
                Path.Combine(
                    "test (2021)",
                    Path.Combine("season 01", $"test (2021) - s01e{episodeNumber:00}.{extension}"))));

        private static string EpisodeNfoName(int episodeNumber = 3) =>
            Path.Combine(
                FakeRoot,
                Path.Combine(
                    "tv",
                    Path.Combine(
                        "test (2021)",
                        Path.Combine("season 01", $"test (2021) - s01e{episodeNumber:00}.nfo"))));

        private static string SeriesPosterNameWithExtension(string extension) =>
            Path.Combine(FakeRoot, Path.Combine("tv", Path.Combine("test (2021)", $"poster.{extension}")));

        [TestFixture]
        public class NewMovieTests
        {
            [Test]
            public void WithoutNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                string movieFileName = MovieNameWithExtension(extension);
                string[] fileNames = { movieFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(movieFileName, ScanningAction.Add),
                    new ActionPlan(movieFileName, ScanningAction.Statistics),
                    new ActionPlan(movieFileName, ScanningAction.FallbackMetadata),
                    new ActionPlan(movieFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                string movieFileName = MovieNameWithExtension(extension);
                string nfoFileName = MovieNfoName(nfoFile);
                string[] fileNames = { movieFileName, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(movieFileName, ScanningAction.Add),
                    new ActionPlan(movieFileName, ScanningAction.Statistics),
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                string movieFileName = MovieNameWithExtension(extension);
                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);

                string[] fileNames = { movieFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(movieFileName, ScanningAction.Add),
                    new ActionPlan(movieFileName, ScanningAction.Statistics),
                    new ActionPlan(movieFileName, ScanningAction.FallbackMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(movieFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                string movieFileName = MovieNameWithExtension(extension);
                string nfoFileName = MovieNfoName(nfoFile);
                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);

                string[] fileNames = { movieFileName, nfoFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(movieFileName, ScanningAction.Add),
                    new ActionPlan(movieFileName, ScanningAction.Statistics),
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class ExistingMovieTests
        {
            [Test]
            public void Old_File_Should_Do_Nothing(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension)
                };

                string[] fileNames = { movieMediaItem.Path };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void Updated_File_Should_Refresh_Statistics(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension)
                };

                string[] fileNames = { movieMediaItem.Path };

                Seq<LocalMediaSourcePlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(movieMediaItem.Path, ScanningAction.Statistics));
            }

            [Test]
            public void Fallback_WithNewNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension)
                };

                string nfoFileName = MovieNfoName(nfoFile);
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void Sidecar_WithOldNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = MovieNameWithExtension(extension)
                };

                string nfoFileName = MovieNfoName(nfoFile);
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void Sidecar_WithUpdatedNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = MovieNameWithExtension(extension)
                };

                string nfoFileName = MovieNfoName(nfoFile);
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result =
                    ScannerFor(OldEntriesFor(movieMediaItem.Path).Concat(NewEntriesFor(nfoFileName)))
                        .DetermineActions(
                            MediaType.Movie,
                            Seq.create(movieMediaItem),
                            fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithNewPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension)
                };

                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ActionPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithoutNfo_WithOldPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension),
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void WithoutNfo_WithUpdatedPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension),
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result =
                    ScannerFor(OldEntriesFor(movieMediaItem.Path).Concat(NewEntriesFor(posterFileName)))
                        .DetermineActions(
                            MediaType.Movie,
                            Seq.create(movieMediaItem),
                            fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ActionPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithNewNfo_WithNewPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = MovieNameWithExtension(extension)
                };

                string nfoFileName = MovieNfoName(nfoFile);
                string posterFileName = MoviePosterNameWithExtension(basePosterName, posterExtension);
                string[] fileNames = { movieMediaItem.Path, nfoFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class NewEpisodeTests
        {
            [Test]
            public void WithoutNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                string episodeFileName = EpisodeNameWithExtension(extension);
                string[] fileNames = { episodeFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(episodeFileName, ScanningAction.Add),
                    new ActionPlan(episodeFileName, ScanningAction.Statistics),
                    new ActionPlan(episodeFileName, ScanningAction.FallbackMetadata),
                    new ActionPlan(episodeFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                string episodeFileName = EpisodeNameWithExtension(extension);
                string nfoFileName = EpisodeNfoName();
                string[] fileNames = { episodeFileName, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(episodeFileName, ScanningAction.Add),
                    new ActionPlan(episodeFileName, ScanningAction.Statistics),
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                string episodeFileName = EpisodeNameWithExtension(extension);
                string posterFileName = SeriesPosterNameWithExtension(posterExtension);

                string[] fileNames = { episodeFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(episodeFileName, ScanningAction.Add),
                    new ActionPlan(episodeFileName, ScanningAction.Statistics),
                    new ActionPlan(episodeFileName, ScanningAction.FallbackMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(episodeFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                string episodeFileName = EpisodeNameWithExtension(extension);
                string nfoFileName = EpisodeNfoName();
                string posterFileName = SeriesPosterNameWithExtension(posterExtension);

                string[] fileNames = { episodeFileName, nfoFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(episodeFileName, ScanningAction.Add),
                    new ActionPlan(episodeFileName, ScanningAction.Statistics),
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class ExistingEpisodeTests
        {
            [Test]
            public void Old_File_Should_Do_Nothing(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension)
                };

                string[] fileNames = { episodeMediaItem.Path };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void Updated_File_Should_Refresh_Statistics(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension)
                };

                string[] fileNames = { episodeMediaItem.Path };

                Seq<LocalMediaSourcePlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(episodeMediaItem.Path, ScanningAction.Statistics));
            }

            [Test]
            public void Fallback_WithNewNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension)
                };

                string nfoFileName = EpisodeNfoName();
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void Sidecar_WithOldNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = EpisodeNameWithExtension(extension)
                };

                string nfoFileName = EpisodeNfoName();
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void Sidecar_WithUpdatedNfo_WithoutPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = EpisodeNameWithExtension(extension)
                };

                string nfoFileName = EpisodeNfoName();
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaSourcePlan> result =
                    ScannerFor(OldEntriesFor(episodeMediaItem.Path).Concat(NewEntriesFor(nfoFileName)))
                        .DetermineActions(
                            MediaType.TvShow,
                            Seq.create(episodeMediaItem),
                            fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithNewPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension)
                };

                string posterFileName = SeriesPosterNameWithExtension(posterExtension);
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ActionPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithoutNfo_WithOldPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension),
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                string posterFileName = SeriesPosterNameWithExtension(posterExtension);
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }

            [Test]
            public void WithoutNfo_WithUpdatedPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension),
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                string posterFileName = SeriesPosterNameWithExtension(posterExtension);
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaSourcePlan> result =
                    ScannerFor(OldEntriesFor(episodeMediaItem.Path).Concat(NewEntriesFor(posterFileName)))
                        .DetermineActions(
                            MediaType.TvShow,
                            Seq.create(episodeMediaItem),
                            fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ActionPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithNewNfo_WithNewPoster(
                [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = EpisodeNameWithExtension(extension)
                };

                string nfoFileName = EpisodeNfoName();
                string posterFileName = SeriesPosterNameWithExtension(posterExtension);
                string[] fileNames = { episodeMediaItem.Path, nfoFileName, posterFileName };

                Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ActionPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ActionPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ActionPlan(posterFileName, ScanningAction.Poster),
                    new ActionPlan(nfoFileName, ScanningAction.Collections));
            }
        }

        [Test]
        public void Movies_Should_Ignore_ExtraFolders(
            [Values(
                "Behind The Scenes",
                "Deleted Scenes",
                "Featurettes",
                "Interviews",
                "Scenes",
                "Shorts",
                "Trailers",
                "Other",
                "Extras",
                "Specials")]
            string folder,
            [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            string[] fileNames =
            {
                Path.Combine(
                    FakeRoot,
                    Path.Combine(
                        "movies",
                        Path.Combine("test (2021)", Path.Combine(folder, $"test (2021).{extension}"))))
            };

            Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }

        [Test]
        public void Movies_Should_Ignore_ExtraFiles(
            [Values(
                "behindthescenes",
                "deleted",
                "featurette",
                "interview",
                "scene",
                "short",
                "trailer",
                "other")]
            string extra,
            [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            string[] fileNames =
            {
                Path.Combine(
                    FakeRoot,
                    Path.Combine(
                        "movies",
                        Path.Combine("test (2021)", $"test (2021)-{extra}.{extension}")))
            };

            Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }

        [Test]
        public void Movies_Should_Remove_Missing_MediaItems(
            [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            var movieMediaItem = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = MovieNameWithExtension(extension)
            };

            var movieMediaItem2 = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = MovieNameWithExtension(extension, 2022)
            };

            string[] fileNames = { "anything" };

            Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq.create(movieMediaItem, movieMediaItem2),
                fileNames.ToSeq());

            result.Count.Should().Be(2);

            (Either<string, MediaItem> source1, List<ActionPlan> itemScanningPlans1) = result.Head();
            source1.IsRight.Should().BeTrue();
            source1.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
            itemScanningPlans1.Should().BeEquivalentTo(
                new ActionPlan(movieMediaItem.Path, ScanningAction.Remove));

            (Either<string, MediaItem> source2, List<ActionPlan> itemScanningPlans2) = result.Last();
            source2.IsRight.Should().BeTrue();
            source2.RightToSeq().Should().BeEquivalentTo(movieMediaItem2);
            itemScanningPlans2.Should().BeEquivalentTo(
                new ActionPlan(movieMediaItem2.Path, ScanningAction.Remove));
        }

        [Test]
        public void Episodes_Should_Remove_Missing_MediaItems(
            [ValueSource(typeof(LocalMediaSourcePlannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            var episodeMediaItem = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = EpisodeNameWithExtension(extension)
            };

            var episodeMediaItem2 = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = EpisodeNameWithExtension(extension, 4)
            };

            string[] fileNames = { "anything" };

            Seq<LocalMediaSourcePlan> result = ScannerForOldFiles(fileNames).DetermineActions(
                MediaType.TvShow,
                Seq.create(episodeMediaItem, episodeMediaItem2),
                fileNames.ToSeq());

            result.Count.Should().Be(2);

            (Either<string, MediaItem> source1, List<ActionPlan> itemScanningPlans1) = result.Head();
            source1.IsRight.Should().BeTrue();
            source1.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
            itemScanningPlans1.Should().BeEquivalentTo(
                new ActionPlan(episodeMediaItem.Path, ScanningAction.Remove));

            (Either<string, MediaItem> source2, List<ActionPlan> itemScanningPlans2) = result.Last();
            source2.IsRight.Should().BeTrue();
            source2.RightToSeq().Should().BeEquivalentTo(episodeMediaItem2);
            itemScanningPlans2.Should().BeEquivalentTo(
                new ActionPlan(episodeMediaItem2.Path, ScanningAction.Remove));
        }

        private class FakeLocalFileSystem : ILocalFileSystem
        {
            private readonly Dictionary<string, DateTime> _files = new();

            public FakeLocalFileSystem(IEnumerable<FakeFileSystemEntry> entries)
            {
                foreach ((string path, DateTime modifyTime) in entries)
                {
                    _files.Add(path, modifyTime);
                }
            }

            public DateTime GetLastWriteTime(string path) =>
                _files.ContainsKey(path) ? _files[path] : DateTime.MinValue;

            public bool IsMediaSourceAccessible(LocalMediaSource localMediaSource) => throw new NotSupportedException();

            public Seq<string> FindRelevantVideos(LocalMediaSource localMediaSource) =>
                throw new NotSupportedException();

            public bool ShouldRefreshMetadata(LocalMediaSource localMediaSource, MediaItem mediaItem) =>
                throw new NotSupportedException();

            public bool ShouldRefreshPoster(MediaItem mediaItem) => throw new NotSupportedException();
        }

        private record FakeFileSystemEntry(string Path, DateTime ModifyTime);
    }
}
