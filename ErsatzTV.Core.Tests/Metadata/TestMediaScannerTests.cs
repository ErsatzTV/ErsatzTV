﻿using System;
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
        private static readonly List<string> VideoFileExtensions = new()
        {
            "mpg", "mp2", "mpeg", "mpe", "mpv", "ogg", "mp4",
            "m4p", "m4v", "avi", "wmv", "mov", "mkv", "ts"
        };

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
            public void WithoutNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var movieFileName = $"/movies/test (2021)/test (2021).{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieFileName = $"/movies/test (2021)/test (2021).{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieFileName = $"/movies/test (2021)/test (2021).{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieFileName = $"/movies/test (2021)/test (2021).{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/movies/test (2021)/test (2021).{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = $"/movies/test (2021)/test (2021).{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("test (2021).nfo", "movie.nfo")]
                string nfoFile)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = $"/movies/test (2021)/test (2021).{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/movies/test (2021)/test (2021).{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/movies/test (2021)/test (2021).{extension}",
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("", "test (2021)-")]
                string basePosterName,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var movieMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/movies/test (2021)/test (2021).{extension}",
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
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
                    Path = $"/movies/test (2021)/test (2021).{extension}"
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
            public void WithoutNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeFileName = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}";
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
            public void WithNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeFileName = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeFileName = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}";
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeFileName = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}";
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
            public void Fallback_WithNewNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}"
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
            public void Sidecar_WithOldNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}"
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
            public void Sidecar_WithUpdatedNfo_WithoutPoster(
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Sidecar },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}"
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}",
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}",
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
                [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
                string extension,
                [Values("jpg", "jpeg", "png", "gif", "tbn")]
                string posterExtension)
            {
                var episodeMediaItem = new MediaItem
                {
                    Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                    Path = $"/tv/test (2021)/season 01/test (2021) - s01e03.{extension}"
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

        public void Movies_Should_Ignore_ExtraFolders(
            [Values("/movies/test (2021)/Behind The Scenes/test (2021)",
                "/movies/test (2021)/Deleted Scenes/test (2021)",
                "/movies/test (2021)/Featurettes/test (2021)",
                "/movies/test (2021)/Interviews/test (2021)",
                "/movies/test (2021)/Scenes/test (2021)",
                "/movies/test (2021)/Shorts/test (2021)",
                "/movies/test (2021)/Trailers/test (2021)",
                "/movies/test (2021)/Other/test (2021)")]
            string baseFileName,
            [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            string[] fileNames = { $"{baseFileName}.{extension}" };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }

        [Test]
        public void Movies_Should_Ignore_ExtraFiles(
            [Values(
                "/movies/test (2021)/test (2021)-behindthescenes",
                "/movies/test (2021)/test (2021)-deleted",
                "/movies/test (2021)/test (2021)-featurette",
                "/movies/test (2021)/test (2021)-interview",
                "/movies/test (2021)/test (2021)-scene",
                "/movies/test (2021)/test (2021)-short",
                "/movies/test (2021)/test (2021)-trailer",
                "/movies/test (2021)/test (2021)-other")]
            string baseFileName,
            [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            string[] fileNames = { $"{baseFileName}.{extension}" };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq<MediaItem>.Empty,
                fileNames.ToSeq());

            result.Count.Should().Be(0);
        }

        [Test]
        // [TestCase(
        //     "/tv/test (2022)/season 03/test (2022) - s03e01.mkv",
        //     "/tv/test (2022)/season 03/test (2022) - s03e02.mkv",
        //     MediaType.TvShow)]
        public void Movies_Should_Remove_Missing_MediaItems(
            [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            var movieMediaItem = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = $"/movies/test (2021)/test (2021).{extension}"
            };

            var movieMediaItem2 = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = $"/movies/test (2022)/test (2022).{extension}"
            };

            string[] fileNames = { "anything" };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.Movie,
                Seq.create(movieMediaItem, movieMediaItem2),
                fileNames.ToSeq());

            result.Count.Should().Be(2);

            (Either<string, MediaItem> source1, List<ItemScanningPlan> itemScanningPlans1) = result.Head();
            source1.IsRight.Should().BeTrue();
            source1.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
            itemScanningPlans1.Should().BeEquivalentTo(
                new ItemScanningPlan(movieMediaItem.Path, ScanningAction.Remove));

            (Either<string, MediaItem> source2, List<ItemScanningPlan> itemScanningPlans2) = result.Last();
            source2.IsRight.Should().BeTrue();
            source2.RightToSeq().Should().BeEquivalentTo(movieMediaItem2);
            itemScanningPlans2.Should().BeEquivalentTo(
                new ItemScanningPlan(movieMediaItem2.Path, ScanningAction.Remove));
        }
        
        [Test]
        public void Episodes_Should_Remove_Missing_MediaItems(
            [ValueSource(typeof(TestMediaScannerTests), nameof(VideoFileExtensions))]
            string extension)
        {
            var movieMediaItem = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = $"/tv/test (2022)/season 03/test (2022) - s03e01.{extension}"
            };

            var movieMediaItem2 = new MediaItem
            {
                Metadata = new MediaMetadata { Source = MetadataSource.Fallback },
                Path = $"/tv/test (2022)/season 03/test (2022) - s03e02.{extension}"
            };

            string[] fileNames = { "anything" };

            Seq<LocalMediaItemScanningPlan> result = ScannerForFiles(fileNames).DetermineActions(
                MediaType.TvShow,
                Seq.create(movieMediaItem, movieMediaItem2),
                fileNames.ToSeq());

            result.Count.Should().Be(2);

            (Either<string, MediaItem> source1, List<ItemScanningPlan> itemScanningPlans1) = result.Head();
            source1.IsRight.Should().BeTrue();
            source1.RightToSeq().Should().BeEquivalentTo(movieMediaItem);
            itemScanningPlans1.Should().BeEquivalentTo(
                new ItemScanningPlan(movieMediaItem.Path, ScanningAction.Remove));

            (Either<string, MediaItem> source2, List<ItemScanningPlan> itemScanningPlans2) = result.Last();
            source2.IsRight.Should().BeTrue();
            source2.RightToSeq().Should().BeEquivalentTo(movieMediaItem2);
            itemScanningPlans2.Should().BeEquivalentTo(
                new ItemScanningPlan(movieMediaItem2.Path, ScanningAction.Remove));
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
