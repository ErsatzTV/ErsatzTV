using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using LanguageExt;

namespace ErsatzTV.Core.Tests.Scheduling;

public abstract class SchedulerTestBase
{
    protected static PlayoutBuilderState StartState(IScheduleItemsEnumerator scheduleItemsEnumerator) => new(
        scheduleItemsEnumerator,
        Prelude.None,
        Prelude.None,
        false,
        false,
        1,
        new DateTimeOffset(new DateTime(2020, 10, 18, 0, 0, 0, DateTimeKind.Local)));

    protected virtual ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
    {
        StartTime = null
    };

    protected static DateTimeOffset HardStop(IScheduleItemsEnumerator scheduleItemsEnumerator) =>
        StartState(scheduleItemsEnumerator).CurrentTime.AddHours(6);

    protected static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
        ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator) =>
        new()
        {
            { CollectionKey.ForScheduleItem(scheduleItem), enumerator }
        };

    protected static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
        ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator1,
        FillerPreset fillerPreset, IMediaCollectionEnumerator enumerator2,
        FillerPreset fillerPreset2, IMediaCollectionEnumerator enumerator3) =>
        new()
        {
            { CollectionKey.ForScheduleItem(scheduleItem), enumerator1 },
            { CollectionKey.ForFillerPreset(fillerPreset), enumerator2 },
            { CollectionKey.ForFillerPreset(fillerPreset2), enumerator3 }
        };

    private static Movie TestMovie(int id, TimeSpan duration, DateTime aired, int chapterCount = 0)
    {
        var result = new Movie()
        {
            Id = id,
            MovieMetadata = new List<MovieMetadata> { new() { ReleaseDate = aired } },
            MediaVersions = new List<MediaVersion>
            {
                new() { Duration = duration, Chapters = new List<MediaChapter>() }
            }
        };

        for (var i = 0; i < chapterCount; i++)
        {
            result.MediaVersions.Head().Chapters.Add(
                new MediaChapter
                {
                    StartTime = TimeSpan.FromMilliseconds(i * duration.TotalMilliseconds / chapterCount),
                    EndTime = TimeSpan.FromMilliseconds(i + 1 * duration.TotalMilliseconds / chapterCount)
                });
        }

        return result;
    }

    protected static Collection TwoItemCollection(int id1, int id2, TimeSpan duration, int chapterCount = 0) => new()
    {
        Id = id1,
        Name = $"Collection of Items {id1}",
        MediaItems = new List<MediaItem>
        {
            TestMovie(id1, duration, new DateTime(2020, 1, 1), chapterCount),
            TestMovie(id2, duration, new DateTime(2020, 1, 2), chapterCount)
        }
    };

    protected static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
        ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator1,
        FillerPreset fillerPreset, IMediaCollectionEnumerator enumerator2) =>
        new()
        {
            { CollectionKey.ForScheduleItem(scheduleItem), enumerator1 },
            { CollectionKey.ForFillerPreset(fillerPreset), enumerator2 }
        };
}