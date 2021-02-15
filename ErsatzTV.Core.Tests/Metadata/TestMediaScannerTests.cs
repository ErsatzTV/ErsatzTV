using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class TestMediaScannerTests
    {
        private static TestMediaScanner ScannerForFiles(IEnumerable<string> fileNames)
        {
            var localFileSystem = new FakeLocalFileSystem(fileNames);
            return new TestMediaScanner(localFileSystem);
        }

        private static TestMediaScanner ScannerForNewFiles(IEnumerable<string> fileNames)
        {
            IEnumerable<FakeFileSystemEntry> fakeFiles =
                fileNames.Map(f => new FakeFileSystemEntry(f, DateTime.MaxValue));
            var localFileSystem = new FakeLocalFileSystem(fakeFiles);
            return new TestMediaScanner(localFileSystem);
        }

        [TestFixture]
        public class NewMovieTests
        {
            [Test]
            public void WithoutNfo_WithoutPoster()
            {
                var movieFileName = "/movies/test (2021)/test (2021).mkv";
                string[] fileNames = { movieFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(movieFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(movieFileName, ScanningAction.FallbackMetadata),
                    new ItemScanningPlan(movieFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithoutPoster(
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieFileName = "/movies/test (2021)/test (2021).mkv";
                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                string[] fileNames = { movieFileName, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(movieFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(movieFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithPoster(
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieFileName = "/movies/test (2021)/test (2021).mkv";
                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";

                string[] fileNames = { movieFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(movieFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(movieFileName, ScanningAction.FallbackMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(movieFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithPoster(
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieFileName = "/movies/test (2021)/test (2021).mkv";
                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";

                string[] fileNames = { movieFileName, nfoFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(movieFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(movieFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(movieFileName, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class ExistingMovieTests
        {
            [Test]
            public void Fallback_WithNewNfo_WithoutPoster(
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/movies/test (2021)/test (2021).mkv"
                };

                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(movieMediaItem.Path, ScanningAction.Collections));
            }

            [Test]
            public void Sidecar_WithOldNfo_WithoutPoster(
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = "/movies/test (2021)/test (2021).mkv"
                };

                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }
            
            [Test]
            public void Sidecar_WithUpdatedNfo_WithoutPoster(
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = "/movies/test (2021)/test (2021).mkv"
                };

                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                string[] fileNames = { movieMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(movieMediaItem.Path, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithNewPoster(
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/movies/test (2021)/test (2021).mkv"
                };

                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ItemScanningPlan(posterFileName, ScanningAction.Poster));
            }
            
            [Test]
            public void WithoutNfo_WithOldPoster(
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/movies/test (2021)/test (2021).mkv",
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }
            
            [Test]
            public void WithoutNfo_WithUpdatedPoster(
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/movies/test (2021)/test (2021).mkv",
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";
                string[] fileNames = { movieMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ItemScanningPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithNewNfo_WithNewPoster(
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
                    Path = "/movies/test (2021)/test (2021).mkv"
                };

                var nfoFileName = $"/movies/test (2021)/{nfoFile}";
                var posterFileName = $"/movies/test (2021)/{basePosterName}poster.{posterExtension}";
                string[] fileNames = { movieMediaItem.Path, nfoFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.Movie,
                    Seq.create(movieMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(movieMediaItem.Path, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class NewEpisodeTests
        {
            [Test]
            public void WithoutNfo_WithoutPoster()
            {
                var episodeFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv";
                string[] fileNames = { episodeFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(episodeFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(episodeFileName, ScanningAction.FallbackMetadata),
                    new ItemScanningPlan(episodeFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithoutPoster()
            {
                var episodeFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv";
                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                string[] fileNames = { episodeFileName, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(episodeFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(episodeFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv";
                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";

                string[] fileNames = { episodeFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(episodeFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(episodeFileName, ScanningAction.FallbackMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(episodeFileName, ScanningAction.Collections));
            }

            [Test]
            public void WithNfo_WithPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv";
                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";

                string[] fileNames = { episodeFileName, nfoFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq<MediaItem>.Empty,
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsLeft.Should().BeTrue();
                source.LeftToSeq().Should().BeEquivalentTo(episodeFileName);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(episodeFileName, ScanningAction.Statistics),
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(episodeFileName, ScanningAction.Collections));
            }
        }

        [TestFixture]
        public class ExistingEpisodeTests
        {
            [Test]
            public void Fallback_WithNewNfo_WithoutPoster()
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(episodeMediaItem.Path, ScanningAction.Collections));
            }
            
            [Test]
            public void Sidecar_WithOldNfo_WithoutPoster()
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }
            
            [Test]
            public void Sidecar_WithUpdatedNfo_WithoutPoster()
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(episodeMediaItem.Path, ScanningAction.Collections));
            }

            [Test]
            public void WithoutNfo_WithNewPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ItemScanningPlan(posterFileName, ScanningAction.Poster));
            }
            
            [Test]
            public void WithoutNfo_WithOldPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv",
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(0);
            }
            
            [Test]
            public void WithoutNfo_WithUpdatedPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv",
                    Poster = "anything",
                    PosterLastWriteTime = DateTime.UtcNow
                };

                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";
                string[] fileNames = { episodeMediaItem.Path, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForNewFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should()
                    .BeEquivalentTo(new ItemScanningPlan(posterFileName, ScanningAction.Poster));
            }

            [Test]
            public void WithNewNfo_WithNewPoster(
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                var posterFileName = $"/tv/test (2021)/poster.{posterExtension}";
                string[] fileNames = { episodeMediaItem.Path, nfoFileName, posterFileName };

                Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                    MediaType.TvShow,
                    Seq.create(episodeMediaItem),
                    fileNames.ToSeq());

                result.Count.Should().Be(1);
                (Either<string, MediaItem> source, List<ItemScanningPlan> itemScanningPlans) = result.Head();
                source.IsRight.Should().BeTrue();
                source.RightToSeq().Should().BeEquivalentTo(episodeMediaItem);
                itemScanningPlans.Should().BeEquivalentTo(
                    new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata),
                    new ItemScanningPlan(posterFileName, ScanningAction.Poster),
                    new ItemScanningPlan(episodeMediaItem.Path, ScanningAction.Collections));
            }
        }

        [Test]
        [TestCase("/movies/test (2021)/Behind The Scenes/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Deleted Scenes/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Featurettes/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Interviews/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Scenes/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Shorts/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Trailers/test (2021).mkv")]
        [TestCase("/movies/test (2021)/Other/test (2021).mkv")]
        public void Movies_Should_Ignore_ExtraFolders(string fileName)
        {
            string[] fileNames = { fileName };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }
        
        [Test]
        [TestCase("/movies/test (2021)/test (2021)-behindthescenes.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-deleted.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-featurette.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-interview.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-scene.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-short.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-trailer.mkv")]
        [TestCase("/movies/test (2021)/test (2021)-other.mkv")]
        public void Movies_Should_Ignore_ExtraFiles(string fileName)
        {
            string[] fileNames = { fileName };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }

        private class FakeLocalFileSystem : ILocalFileSystem
        {
            private readonly Dictionary<string, DateTime> _files = new();

            public FakeLocalFileSystem(IEnumerable<string> files)
            {
                // default to being old/unmodified
                foreach (string file in files)
                {
                    _files.Add(file, DateTime.MinValue);
                }
            }

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
