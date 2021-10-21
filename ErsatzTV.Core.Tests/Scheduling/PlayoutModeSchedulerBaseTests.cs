using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling
{
    [TestFixture]
    public class PlayoutModeSchedulerBaseTests
    {
        [Test]
        public void CalculateEndTimeWithFiller_Should_Not_Touch_Enumerator()
        {
            var collection = new Collection
            {
                Id = 1,
                Name = "Filler Items",
                MediaItems = new List<MediaItem>()
            };

            for (var i = 0; i < 5; i++)
            {
                collection.MediaItems.Add(TestMovie(i + 1, TimeSpan.FromHours(i + 1), new DateTime(2020, 2, i + 1)));
            }

            var fillerPreset = new FillerPreset
            {
                FillerKind = FillerKind.MidRoll,
                FillerMode = FillerMode.Count,
                Count = 3,
                Collection = collection,
                CollectionId = collection.Id
            };

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collection.MediaItems,
                new CollectionEnumeratorState { Index = 0, Seed = 1 });

            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>
                    {
                        { CollectionKey.ForFillerPreset(fillerPreset), enumerator }
                    },
                    new ProgramScheduleItemOne
                    {
                        PreRollFiller = fillerPreset
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 18, 12, 30, TimeSpan.FromHours(-5)));
            enumerator.State.Index.Should().Be(0);
            enumerator.State.Seed.Should().Be(1);
        }

        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_15_Minutes_15()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 15, 0, TimeSpan.FromHours(-5)));
        }
        
        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_15_Minutes_30()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 16, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)));
        }
        
        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_15_Minutes_45()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 45, 0, TimeSpan.FromHours(-5)));
        }
        
        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_15_Minutes_00()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 46, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 13, 0, 0, TimeSpan.FromHours(-5)));
        }
        
        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_30_Minutes_30()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 30
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)));
        }
        
        [Test]
        public void CalculateEndTimeWithFiller_Should_Pad_To_30_Minutes_00()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 30
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 20, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30));

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 13, 0, 0, TimeSpan.FromHours(-5)));
        }

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
    }
}
