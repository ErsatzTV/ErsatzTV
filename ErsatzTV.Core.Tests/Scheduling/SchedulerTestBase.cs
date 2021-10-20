using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using LanguageExt;

namespace ErsatzTV.Core.Tests.Scheduling
{
    public abstract class SchedulerTestBase
    {
        protected static PlayoutBuilderState StartState => new(
            0,
            Prelude.None,
            Prelude.None,
            false,
            false,
            false,
            new DateTimeOffset(2020, 10, 18, 0, 0, 0, TimeSpan.FromHours(-5)));

        protected virtual ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
        {
            StartTime = null
        };

        protected static DateTimeOffset HardStop => StartState.CurrentTime.AddHours(6);

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

        private static Movie TestMovie(int id, TimeSpan duration, DateTime aired) =>
            new()
            {
                Id = id,
                MovieMetadata = new List<MovieMetadata> { new() { ReleaseDate = aired } },
                MediaVersions = new List<MediaVersion>
                {
                    new() { Duration = duration }
                }
            };

        protected static Collection TwoItemCollection(int id1, int id2, TimeSpan duration) => new()
        {
            Id = id1,
            Name = $"Duration Items {id1}",
            MediaItems = new List<MediaItem>
            {
                TestMovie(id1, duration, new DateTime(2020, 1, 1)),
                TestMovie(id2, duration, new DateTime(2020, 1, 2))
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
}
