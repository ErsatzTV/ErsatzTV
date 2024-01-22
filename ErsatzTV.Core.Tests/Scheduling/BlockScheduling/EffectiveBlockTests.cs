using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling.BlockScheduling;

public static class EffectiveBlockTests
{
    [TestFixture]
    public class GetEffectiveBlocks
    {
        [Test]
        public void Should_Work_With_No_Matching_Days()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            List<PlayoutTemplate> templates =
            [
                new PlayoutTemplate
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

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, start, daysToBuild: 5);

            result.Should().HaveCount(0);
        }
        
        [Test]
        public void Should_Work_With_Blank_Days()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            List<PlayoutTemplate> templates =
            [
                new PlayoutTemplate
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

            List<EffectiveBlock> result = EffectiveBlock.GetEffectiveBlocks(templates, start, daysToBuild: 5);

            result.Should().HaveCount(3);
            
            result[0].Start.DayOfWeek.Should().Be(DayOfWeek.Monday);
            result[0].Start.Date.Should().Be(GetLocalDate(2024, 1, 15).Date);
            
            result[1].Start.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
            result[1].Start.Date.Should().Be(GetLocalDate(2024, 1, 17).Date);

            result[2].Start.DayOfWeek.Should().Be(DayOfWeek.Friday);
            result[2].Start.Date.Should().Be(GetLocalDate(2024, 1, 19).Date);
        }
        
        // TODO: test when clocks spring forward
        // TODO: test when clocks fall back
        
        // TODO: offset may be incorrect on days with time change, since start offset is re-used
    }

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
}
