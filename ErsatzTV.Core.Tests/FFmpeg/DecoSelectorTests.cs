using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.FFmpeg;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Shouldly;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class DecoSelectorTests
{
    private static readonly DecoSelector DecoSelector;

    static DecoSelectorTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        DecoSelector = new DecoSelector(loggerFactory.CreateLogger<DecoSelector>());
    }

    [Test]
    public void GetDecoEntries_Should_Not_Select_Deco_Before_Start_Time()
    {
        var deco = new Deco { Id = 1, Name = "Test Deco" };

        var decoTemplateItem = new DecoTemplateItem
        {
            Id = 1,
            DecoId = 1,
            Deco = deco,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(18, 0, 0)
        };

        var decoTemplate = new DecoTemplate
        {
            Id = 1,
            Name = "Test Deco Template",
            Items = new List<DecoTemplateItem> { decoTemplateItem }
        };

        var playoutTemplate = new PlayoutTemplate
        {
            Id = 1,
            Template = new Template { Id = 1, Name = "Test Template" },
            DecoTemplate = decoTemplate,
            DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
            DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
            MonthsOfYear = PlayoutTemplate.AllMonthsOfYear()
        };

        var playout = new Playout
        {
            Id = 1,
            Templates = new List<PlayoutTemplate> { playoutTemplate }
        };

        var now = new DateTimeOffset(2025, 9, 9, 9, 0, 0, TimeSpan.FromHours(-5));

        var result = DecoSelector.GetDecoEntries(playout, now);

        result.TemplateDeco.IsNone.ShouldBeTrue();
    }

    [Test]
    public void GetDecoEntries_Should_Select_Correct_Deco_From_Multiple()
    {
        var deco1 = new Deco { Id = 1, Name = "Test Deco 1" };
        var deco2 = new Deco { Id = 2, Name = "Test Deco 2" };

        var decoTemplateItem1 = new DecoTemplateItem
        {
            Id = 1,
            DecoId = 1,
            Deco = deco1,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0)
        };

        var decoTemplateItem2 = new DecoTemplateItem
        {
            Id = 2,
            DecoId = 2,
            Deco = deco2,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(18, 0, 0)
        };

        var decoTemplate = new DecoTemplate
        {
            Id = 1,
            Name = "Test Deco Template",
            Items = new List<DecoTemplateItem> { decoTemplateItem1, decoTemplateItem2 }
        };

        var playoutTemplate = new PlayoutTemplate
        {
            Id = 1,
            Template = new Template { Id = 1, Name = "Test Template" },
            DecoTemplate = decoTemplate,
            DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
            DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
            MonthsOfYear = PlayoutTemplate.AllMonthsOfYear()
        };

        var playout = new Playout
        {
            Id = 1,
            Templates = new List<PlayoutTemplate> { playoutTemplate }
        };

        var now = new DateTimeOffset(2025, 9, 9, 9, 0, 0, TimeSpan.FromHours(-5));

        var result = DecoSelector.GetDecoEntries(playout, now);

        result.TemplateDeco.IsSome.ShouldBeTrue();
        result.TemplateDeco.IfSome(d => d.ShouldBe(deco1));
    }
}
