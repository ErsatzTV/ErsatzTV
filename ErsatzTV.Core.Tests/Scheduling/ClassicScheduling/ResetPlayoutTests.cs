using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.ClassicScheduling;

[TestFixture]
public class ResetPlayoutTests : PlayoutBuilderTestBase
{
    [Test]
    public async Task ShuffleFlood_Should_IgnoreAnchors()
    {
        var mediaItems = new List<MediaItem>
        {
            TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
            TestMovie(2, TimeSpan.FromHours(1), DateTime.Today.AddHours(1)),
            TestMovie(3, TimeSpan.FromHours(1), DateTime.Today.AddHours(2)),
            TestMovie(4, TimeSpan.FromHours(1), DateTime.Today.AddHours(3)),
            TestMovie(5, TimeSpan.FromHours(1), DateTime.Today.AddHours(4)),
            TestMovie(6, TimeSpan.FromHours(1), DateTime.Today.AddHours(5))
        };

        (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
            TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
        DateTimeOffset start = HoursAfterMidnight(0);
        DateTimeOffset finish = start + TimeSpan.FromHours(6);

        PlayoutBuildResult result = await builder.Build(
            playout,
            referenceData,
            PlayoutBuildResult.Empty,
            PlayoutBuildMode.Reset,
            start,
            finish,
            CancellationToken);

        result.AddedItems.Count.ShouldBe(6);
        playout.Anchor.NextStartOffset.ShouldBe(finish);

        playout.ProgramScheduleAnchors.Count.ShouldBe(1);
        playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

        int firstSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

        DateTimeOffset start2 = HoursAfterMidnight(0);
        DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

        PlayoutBuildResult result2 = await builder.Build(
            playout,
            referenceData,
            PlayoutBuildResult.Empty,
            PlayoutBuildMode.Reset,
            start2,
            finish2,
            CancellationToken);

        result2.AddedItems.Count.ShouldBe(6);
        playout.Anchor.NextStartOffset.ShouldBe(finish);

        playout.ProgramScheduleAnchors.Count.ShouldBe(1);
        playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

        int secondSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

        firstSeedValue.ShouldNotBe(secondSeedValue);
    }
}
