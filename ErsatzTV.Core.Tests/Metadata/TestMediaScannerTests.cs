using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class TestMediaScannerTests
    {
        [TestFixture]
        public class NewMovieTests
        {
            private readonly TestMediaScanner _scanner = new();

            [Test]
            public void WithoutNfo_WithoutPoster()
            {
                var movieFileName = "/movies/test (2021)/test (2021).mkv";
                string[] fileNames = { movieFileName };

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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
            private readonly TestMediaScanner _scanner = new();

            // TODO: mtime should affect this
            [Test]
            public void WithNewNfo_WithoutPoster(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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
            private readonly TestMediaScanner _scanner = new();

            [Test]
            public void WithoutNfo_WithoutPoster()
            {
                var episodeFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv";
                string[] fileNames = { episodeFileName };

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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
            private readonly TestMediaScanner _scanner = new();

            // TODO: mtime should affect this
            [Test]
            public void WithNewNfo_WithoutPoster()
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = "/tv/test (2021)/season 01/test (2021) - s01e03.mkv"
                };

                var nfoFileName = "/tv/test (2021)/season 01/test (2021) - s01e03.nfo";
                string[] fileNames = { episodeMediaItem.Path, nfoFileName };

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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

                Seq<LocalMediaItemScanningPlan> result = _scanner.DetermineActions(
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
    }
}
