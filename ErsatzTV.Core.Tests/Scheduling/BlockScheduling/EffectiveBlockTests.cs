using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.BlockScheduling;

public static class EffectiveBlockTests
{
    private static DateTimeOffset GetLocalDate(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.FromHours(-6));

    private static Template SingleBlockTemplate(DateTimeOffset dateUpdated)
    {
        var template = new Template
        {
            Id = 1,
            Items =
            [
                new TemplateItem
                {
                    Block = new Block
                    {
                        Id = 1,
                        DateUpdated = dateUpdated.UtcDateTime
                    },
                    StartTime = TimeSpan.FromHours(9)
                }
            ],
            DateUpdated = dateUpdated.UtcDateTime
        };

        // this is used for navigation
        foreach (TemplateItem item in template.Items)
        {
            item.Template = template;
        }

        return template;
    }

    [TestFixture]
    public class GetEffectiveBlocks
    {
        [Test]
        public void Should_Work_With_No_Matching_Days()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            List<PlayoutTemplate> templates =
            [
                new()
                {
                    Index = 1,
                    DaysOfWeek = [DayOfWeek.Sunday],
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    Template = SingleBlockTemplate(now),
                    DateUpdated = now.UtcDateTime
                }
            ];

            DateTimeOffset start = GetLocalDate(2024, 1, 15).AddHours(9);

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, start, 5);

            result.Count.ShouldBe(0);
        }

        [Test]
        public void Should_Work_With_Blank_Days()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            List<PlayoutTemplate> templates =
            [
                new()
                {
                    Index = 1,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    Template = SingleBlockTemplate(now),
                    DateUpdated = now.UtcDateTime
                }
            ];

            DateTimeOffset start = GetLocalDate(2024, 1, 15).AddHours(9);

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, start, 5);

            result.Count.ShouldBe(3);

            result[0].Start.DayOfWeek.ShouldBe(DayOfWeek.Monday);
            result[0].Start.Date.ShouldBe(GetLocalDate(2024, 1, 15).Date);

            result[1].Start.DayOfWeek.ShouldBe(DayOfWeek.Wednesday);
            result[1].Start.Date.ShouldBe(GetLocalDate(2024, 1, 17).Date);

            result[2].Start.DayOfWeek.ShouldBe(DayOfWeek.Friday);
            result[2].Start.Date.ShouldBe(GetLocalDate(2024, 1, 19).Date);
        }

        [Test]
        public void Should_Handle_Spring_Forward()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            List<PlayoutTemplate> templates =
            [
                new()
                {
                    Index = 1,
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    Template = SingleBlockTemplate(now), // 9am block
                    DateUpdated = now.UtcDateTime
                }
            ];

            // In 2024, DST starts on March 10 for America/Chicago
            // For Windows, this would be "Central Standard Time"
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            var start = new DateTime(2024, 3, 9, 0, 0, 0, DateTimeKind.Unspecified);
            var dto = new DateTimeOffset(start, tz.GetUtcOffset(start));

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, dto, 5);

            result.Count.ShouldBe(5);

            // Saturday March 9, 9am is CST (-6)
            var blockOnSat = result.Single(r => r.Start.Day == 9);
            blockOnSat.Start.Hour.ShouldBe(9);
            blockOnSat.Start.Offset.ShouldBe(TimeSpan.FromHours(-6));

            // Sunday March 10, 9am is CDT (-5)
            var blockOnSun = result.Single(r => r.Start.Day == 10);
            blockOnSun.Start.Hour.ShouldBe(9);
            blockOnSun.Start.Offset.ShouldBe(TimeSpan.FromHours(-5));
        }

        [Test]
        public void Should_Handle_Fall_Back()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            List<PlayoutTemplate> templates =
            [
                new()
                {
                    Index = 1,
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    Template = SingleBlockTemplate(now), // 9am block
                    DateUpdated = now.UtcDateTime
                }
            ];

            // In 2024, DST ends on Nov 3 for America/Chicago
            // For Windows, this would be "Central Standard Time"
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            var start = new DateTime(2024, 11, 2, 0, 0, 0, DateTimeKind.Unspecified);
            var dto = new DateTimeOffset(start, tz.GetUtcOffset(start));

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, dto, 5);

            result.Count.ShouldBe(5);

            // Saturday Nov 2, 9am is CDT (-5)
            var blockOnSat = result.Single(r => r.Start.Day == 2);
            blockOnSat.Start.Hour.ShouldBe(9);
            blockOnSat.Start.Offset.ShouldBe(TimeSpan.FromHours(-5));

            // Sunday Nov 3, 9am is CST (-6)
            var blockOnSun = result.Single(r => r.Start.Day == 3);
            blockOnSun.Start.Hour.ShouldBe(9);
            blockOnSun.Start.Offset.ShouldBe(TimeSpan.FromHours(-6));
        }
    }
}
