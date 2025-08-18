using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Shouldly;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class WatermarkSelectorTests
{
    private static readonly WatermarkSelector WatermarkSelector;
    private static readonly Option<ChannelWatermark> WatermarkNone;
    private static readonly ChannelWatermark WatermarkGlobal;
    private static readonly ChannelWatermark WatermarkChannel;
    private static readonly ChannelWatermark WatermarkPlayoutItem;
    private static readonly ChannelWatermark WatermarkTemplateDeco;
    private static readonly ChannelWatermark WatermarkDefaultDeco;
    private static readonly Channel ChannelWithWatermark;
    private static readonly Channel ChannelNoWatermark;
    private static readonly DateTimeOffset Now = new(2025, 08, 17, 12, 0, 0, TimeSpan.FromHours(-5));

    private static readonly PlayoutItem PlayoutItemDisableWatermarks =
        new() { Watermarks = [], DisableWatermarks = true };

    private static readonly PlayoutItem PlayoutItemWithNoWatermarks =
        new() { Watermarks = [], DisableWatermarks = false };

    private static readonly PlayoutItem PlayoutItemWithWatermark;

    private static readonly PlayoutItem PlayoutItemWithDisabledWatermark;

    private static readonly PlayoutItem TemplateDecoInherit;
    private static readonly PlayoutItem TemplateDecoDisable;
    private static readonly PlayoutItem TemplateDecoInheritWithWatermark;
    private static readonly PlayoutItem TemplateDecoDisableWithWatermark;
    private static readonly PlayoutItem TemplateDecoOverrideWithWatermark;
    private static readonly PlayoutItem TemplateDecoInheritDefaultDecoOverrideWithWatermark;

    private static readonly PlayoutItem DefaultDecoInherit;
    private static readonly PlayoutItem DefaultDecoDisable;
    private static readonly PlayoutItem DefaultDecoInheritWithWatermark;
    private static readonly PlayoutItem DefaultDecoDisableWithWatermark;
    private static readonly PlayoutItem DefaultDecoOverrideWithWatermark;

    private static readonly List<ChannelWatermark> WatermarkResultEmpty = [];

    static WatermarkSelectorTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        WatermarkSelector = new WatermarkSelector(
            Substitute.For<IImageCache>(),
            loggerFactory.CreateLogger<WatermarkSelector>());

        WatermarkNone = Option<ChannelWatermark>.None;

        WatermarkGlobal = new ChannelWatermark { Id = 0, Name = "Global", Image = "GlobalImage" };

        WatermarkChannel = new ChannelWatermark { Id = 1, Name = "Channel", Image = "ChannelImage" };
        WatermarkPlayoutItem = new ChannelWatermark { Id = 2, Name = "PlayoutItem", Image = "PlayoutItemImage" };
        WatermarkTemplateDeco = new ChannelWatermark { Id = 3, Name = "TemplateDeco", Image = "TemplateDecoImage" };
        WatermarkDefaultDeco = new ChannelWatermark { Id = 4, Name = "DefaultDeco", Image = "DefaultDecoImage" };

        ChannelWithWatermark = new Channel(Guid.Empty)
            { Id = 0, Watermark = WatermarkChannel, WatermarkId = WatermarkChannel.Id };

        ChannelNoWatermark = new Channel(Guid.Empty) { Id = 0, Watermark = null, WatermarkId = null };

        PlayoutItemWithWatermark = new PlayoutItem { Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false };

        PlayoutItemWithDisabledWatermark = new PlayoutItem
            { Watermarks = [WatermarkPlayoutItem], DisableWatermarks = true };

        var decoWithInherit = new DecoTemplate
        {
            Items = [new DecoTemplateItem { Deco = new Deco { WatermarkMode = DecoMode.Inherit } }]
        };

        var decoWithDisable = new DecoTemplate
        {
            Items = [new DecoTemplateItem { Deco = new Deco { WatermarkMode = DecoMode.Disable } }]
        };

        var decoWithOverride = new DecoTemplate
        {
            Items =
            [
                new DecoTemplateItem
                {
                    Deco = new Deco
                    {
                        WatermarkMode = DecoMode.Override,
                        DecoWatermarks = [new DecoWatermark { Watermark = WatermarkTemplateDeco }]
                    }
                }
            ]
        };

        var playoutWithTemplateDecoInherit = new Playout
        {
            Templates =
            [
                new PlayoutTemplate
                {
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    DecoTemplate = decoWithInherit
                }
            ]
        };

        var playoutWithTemplateDecoDisable = new Playout
        {
            Templates =
            [
                new PlayoutTemplate
                {
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    DecoTemplate = decoWithDisable
                }
            ]
        };

        var playoutWithTemplateDecoOverride = new Playout
        {
            Templates =
            [
                new PlayoutTemplate
                {
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    DecoTemplate = decoWithOverride
                }
            ]
        };

        TemplateDecoInherit = new PlayoutItem { Playout = playoutWithTemplateDecoInherit, Watermarks = [] };

        TemplateDecoDisable = new PlayoutItem
            { Watermarks = [], DisableWatermarks = false, Playout = playoutWithTemplateDecoDisable };

        TemplateDecoInheritWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithTemplateDecoInherit
        };

        TemplateDecoDisableWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithTemplateDecoDisable
        };

        TemplateDecoOverrideWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithTemplateDecoOverride
        };

        var playoutWithDecoInherit = new Playout
        {
            Deco = new Deco { WatermarkMode = DecoMode.Inherit },
            Templates = []
        };

        var playoutWithDecoDisable = new Playout
        {
            Deco = new Deco { WatermarkMode = DecoMode.Disable },
            Templates = []
        };

        var playoutWithDecoOverride = new Playout
        {
            Deco = new Deco
            {
                WatermarkMode = DecoMode.Override,
                DecoWatermarks = [new DecoWatermark { Watermark = WatermarkDefaultDeco }]
            },
            Templates = []
        };

        DefaultDecoInherit = new PlayoutItem { Playout = playoutWithDecoInherit, Watermarks = [] };

        DefaultDecoDisable = new PlayoutItem
            { Watermarks = [], DisableWatermarks = false, Playout = playoutWithDecoDisable };

        DefaultDecoInheritWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithDecoInherit
        };

        DefaultDecoDisableWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithDecoDisable
        };

        DefaultDecoOverrideWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem], DisableWatermarks = false, Playout = playoutWithDecoOverride
        };

        var playoutWithTemplateDecoInheritDefaultDecoOverride = new Playout
        {
            Templates =
            [
                new PlayoutTemplate
                {
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    DecoTemplate = decoWithInherit
                }
            ],
            Deco = new Deco
            {
                WatermarkMode = DecoMode.Override,
                DecoWatermarks = [new DecoWatermark { Watermark = WatermarkDefaultDeco }]
            },
        };

        TemplateDecoInheritDefaultDecoOverrideWithWatermark = new PlayoutItem
        {
            Watermarks = [WatermarkPlayoutItem],
            DisableWatermarks = false,
            Playout = playoutWithTemplateDecoInheritDefaultDecoOverride
        };

    }

    private static IEnumerable<(Option<ChannelWatermark>, Channel, PlayoutItem, List<ChannelWatermark>)>
        SelectWatermarksTestCases()
    {
        // STANDARD --------------------------------------------

        // no watermark when none are configured
        yield return (WatermarkNone, ChannelNoWatermark, PlayoutItemWithNoWatermarks, WatermarkResultEmpty);

        // no watermark when global configured but disabled playout item
        yield return (WatermarkGlobal, ChannelNoWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // global watermark when global configured
        yield return (WatermarkGlobal, ChannelNoWatermark, PlayoutItemWithNoWatermarks, [WatermarkGlobal]);

        // channel watermark when global and channel configured
        yield return (WatermarkGlobal, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [WatermarkChannel]);

        // channel watermark when channel configured
        yield return (WatermarkNone, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [WatermarkChannel]);

        // playout item when global, channel and playout item configured
        yield return (WatermarkGlobal, ChannelWithWatermark, PlayoutItemWithWatermark, [WatermarkPlayoutItem]);

        // playout item when channel and playout item configured
        yield return (WatermarkNone, ChannelWithWatermark, PlayoutItemWithWatermark, [WatermarkPlayoutItem]);

        // playout item when playout item configured
        yield return (WatermarkNone, ChannelNoWatermark, PlayoutItemWithWatermark, [WatermarkPlayoutItem]);

        // no watermark when channel configured with playout item disabled
        yield return (WatermarkNone, ChannelWithWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // no watermark when global and channel configured with playout item disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // no watermark when global, channel and playout item configured with playout item disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, PlayoutItemWithDisabledWatermark, WatermarkResultEmpty);

        // PLAYOUT TEMPLATE DECO -------------------------------

        // no watermark when global, channel and playout item configured with template deco disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoDisableWithWatermark, WatermarkResultEmpty);

        // no watermark when global, channel configured with template deco disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoDisable, WatermarkResultEmpty);

        // no watermark when global configured with template deco disabled
        yield return (WatermarkGlobal, ChannelNoWatermark, TemplateDecoDisable, WatermarkResultEmpty);

        // playout item when global, channel and playout item configured with template deco inherit
        yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoInheritWithWatermark, [WatermarkPlayoutItem]);

        // channel when global, channel configured with template deco inherit
        yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoInherit, [WatermarkChannel]);

        // global when global configured with template deco inherit
        yield return (WatermarkGlobal, ChannelNoWatermark, TemplateDecoInherit, [WatermarkGlobal]);

        // no watermark when none configured with template deco inherit
        yield return (WatermarkNone, ChannelNoWatermark, TemplateDecoInherit, WatermarkResultEmpty);

        // PLAYOUT DEFAULT DECO --------------------------------

        // no watermark when global, channel and playout item configured with default deco disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, DefaultDecoDisableWithWatermark, WatermarkResultEmpty);

        // no watermark when global, channel configured with default deco disabled
        yield return (WatermarkGlobal, ChannelWithWatermark, DefaultDecoDisable, WatermarkResultEmpty);

        // no watermark when global configured with default deco disabled
        yield return (WatermarkGlobal, ChannelNoWatermark, DefaultDecoDisable, WatermarkResultEmpty);

        // playout item when global, channel and playout item configured with default deco inherit
        yield return (WatermarkGlobal, ChannelWithWatermark, DefaultDecoInheritWithWatermark, [WatermarkPlayoutItem]);

        // channel when global, channel configured with default deco inherit
        yield return (WatermarkGlobal, ChannelWithWatermark, DefaultDecoInherit, [WatermarkChannel]);

        // global when global configured with default deco inherit
        yield return (WatermarkGlobal, ChannelNoWatermark, DefaultDecoInherit, [WatermarkGlobal]);

        // no watermark when none configured with default deco inherit
        yield return (WatermarkNone, ChannelNoWatermark, DefaultDecoInherit, WatermarkResultEmpty);

        // PLAYOUT TEMPLATE AND DEFAULT DECO -------------------

        // default deco when global, channel and playout item configured with default deco override, template deco inherit
        yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoInheritDefaultDecoOverrideWithWatermark, [WatermarkDefaultDeco]);
    }

    [TestCaseSource(nameof(SelectWatermarksTestCases))]
    public void Should_Select_Appropriate_Watermark(
        (Option<ChannelWatermark> globalWatermark,
        Channel channel,
        PlayoutItem playoutItem,
        List<ChannelWatermark> expectedWatermarks) td)
    {
        List<WatermarkOptions> watermarks = WatermarkSelector.SelectWatermarks(
            td.globalWatermark,
            td.channel,
            td.playoutItem,
            Now);

        watermarks.Count.ShouldBe(td.expectedWatermarks.Count);
        for (var i = 0; i < td.expectedWatermarks.Count; i++)
        {
            watermarks[i].Watermark.ShouldBe(td.expectedWatermarks[i]);
        }
    }

    // TODO: also decos?
}
