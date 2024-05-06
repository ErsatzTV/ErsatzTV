using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

public static class PlayoutTemplateSelectorTests
{
    [TestFixture]
    public class GetPlayoutTemplateFor
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

        [Test]
        public void LimitToDateRange_Before_Start_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 4,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 3, 31, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_On_Start_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 4,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 4, 1, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_In_Range_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 4,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 4, 20, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_On_End_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 4,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 6, 15, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_After_End_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 4,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 6, 16, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Before_Invalid_Start_Date_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 2,
                StartDay = 29,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 2, 28, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_After_Invalid_Start_Date_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 2,
                StartDay = 29,
                EndMonth = 6,
                EndDay = 15
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 3, 1, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Before_Invalid_End_Date_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 1,
                StartDay = 1,
                EndMonth = 2,
                EndDay = 29
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 2, 28, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_After_Invalid_End_Date_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 1,
                StartDay = 1,
                EndMonth = 2,
                EndDay = 29
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 3, 1, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_Before_Start_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 6,
                StartDay = 15,
                EndMonth = 4,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 6, 14, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_On_Start_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 6,
                StartDay = 15,
                EndMonth = 4,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 6, 15, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_In_Range_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 6,
                StartDay = 15,
                EndMonth = 4,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 7, 20, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_On_End_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 6,
                StartDay = 15,
                EndMonth = 4,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 4, 1, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_After_End_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 6,
                StartDay = 15,
                EndMonth = 4,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2024, 4, 2, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_Before_Invalid_Start_Date_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 2,
                StartDay = 29,
                EndMonth = 1,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 2, 28, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_After_Invalid_Start_Date_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 2,
                StartDay = 29,
                EndMonth = 1,
                EndDay = 1
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 3, 1, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_Before_Invalid_End_Date_Should_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 5,
                StartDay = 1,
                EndMonth = 2,
                EndDay = 29
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 2, 28, 0, 0, 0, Offset));

            result.IsSome.Should().BeTrue();
        }

        [Test]
        public void LimitToDateRange_Wrap_Around_After_Invalid_End_Date_Should_Not_Return_Template()
        {
            var template = new PlayoutTemplate
            {
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                LimitToDateRange = true,
                StartMonth = 5,
                StartDay = 1,
                EndMonth = 2,
                EndDay = 29
            };

            Option<PlayoutTemplate> result = PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new List<PlayoutTemplate> { template },
                new DateTimeOffset(2023, 3, 1, 0, 0, 0, Offset));

            result.IsNone.Should().BeTrue();
        }
    }
}
