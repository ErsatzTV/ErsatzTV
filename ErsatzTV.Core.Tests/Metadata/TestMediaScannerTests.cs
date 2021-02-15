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
        private readonly TestMediaScanner _scanner = new();

        [Test]
        public void NewMovieFile_WithoutNfo_WithoutPoster()
        {
            // new movie file without nfo and without poster should have statistics and fallback metadata
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
                new ItemScanningPlan(movieFileName, ScanningAction.FallbackMetadata));
        }
        
        [Test]
        public void NewMovieFile_WithNfo_WithoutPoster(
            [Values("test (2021).nfo", "movie.nfo")]
            string nfoFile)
        {
            // new movie file without nfo and without poster should have statistics and fallback metadata
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
                new ItemScanningPlan(nfoFileName, ScanningAction.SidecarMetadata));
        }
        
        [Test]
        public void NewMovieFile_WithoutNfo_WithPoster(
            [Values("", "test (2021)-")]
            string basePosterName,
            [Values("jpg", "jpeg", "png", "gif", "tbn")]
            string posterExtension)
        {
            // new movie file without nfo and without poster should have statistics and fallback metadata
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
                new ItemScanningPlan(posterFileName, ScanningAction.Poster));
        }
        
        [Test]
        public void NewMovieFile_WithNfo_WithPoster(
            [Values("test (2021).nfo", "movie.nfo")]
            string nfoFile,
            [Values("", "test (2021)-")]
            string basePosterName,
            [Values("jpg", "jpeg", "png", "gif", "tbn")]
            string posterExtension)
        {
            // new movie file without nfo and without poster should have statistics and fallback metadata
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
                new ItemScanningPlan(posterFileName, ScanningAction.Poster));
        }
    }
}
