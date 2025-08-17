using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
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
    private static readonly Option<ChannelWatermark> WatermarkGlobal;
    private static readonly Option<ChannelWatermark> WatermarkNone;
    private static readonly Option<ChannelWatermark> WatermarkChannel;
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

        WatermarkGlobal = new ChannelWatermark { Id = 0, Name = "Global", Image = "GlobalImage" };
        WatermarkNone = Option<ChannelWatermark>.None;

        WatermarkChannel = new ChannelWatermark { Id = 1, Name = "Channel", Image = "ChannelImage" };
        WatermarkPlayoutItem = new ChannelWatermark { Id = 2, Name = "PlayoutItem", Image = "PlayoutItemImage" };
        WatermarkTemplateDeco = new ChannelWatermark { Id = 3, Name = "TemplateDeco", Image = "TemplateDecoImage" };
        WatermarkDefaultDeco = new ChannelWatermark { Id = 4, Name = "DefaultDeco", Image = "DefaultDecoImage" };

        foreach (var channelWatermark in WatermarkChannel)
        {
            ChannelWithWatermark = new Channel(Guid.Empty)
                { Id = 0, Watermark = channelWatermark, WatermarkId = channelWatermark.Id };
        }

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

    [Test]
    public void Should_Select_No_Watermark_When_None_Are_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_Configured_But_Disabled_PlayoutItem()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemDisableWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public void Should_Select_Global_Watermark_When_Global_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkGlobal);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Global_And_Channel_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkChannel);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Channel_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkChannel);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Channel_Configured_But_Disabled_PlayoutItem()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemDisableWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public void Should_Select_PlayoutItem_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkPlayoutItem);
    }

    [Test]
    public void Should_Select_PlayoutItem_Watermark_When_Channel_And_PlayoutItem_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkPlayoutItem);
    }

    [Test]
    public void Should_Select_PlayoutItem_Watermark_When_PlayoutItem_Configured()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkPlayoutItem);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_But_Disabled_PlayoutItem()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithDisabledWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_But_Disabled_Template_Deco()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoDisableWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_Configured_But_Disabled_Template_Deco()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoDisable;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_Template_Deco_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_With_Template_Deco_Override()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoOverrideWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<CustomWatermarks>();

        playoutItem.Watermarks.Clear();
        playoutItem.Watermarks.AddRange(((CustomWatermarks)playoutItemWatermark).Watermarks);

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkTemplateDeco);
    }

    [Test]
    public void Should_Select_Playout_Item_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_With_Template_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoInheritWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkPlayoutItem);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Global_And_Channel_Configured_With_Template_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkChannel);
    }

    [Test]
    public void Should_Select_Global_Watermark_When_Global_Configured_With_Template_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelNoWatermark;
        var playoutItem = TemplateDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkGlobal);
    }

    [Test]
    public void Should_Select_No_Watermark_When_None_Configured_With_Template_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelNoWatermark;
        var playoutItem = TemplateDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_But_Disabled_Default_Deco()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = DefaultDecoDisableWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_Configured_But_Disabled_Default_Deco()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = DefaultDecoDisable;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_Default_Deco_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_With_Default_Deco_Override()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = DefaultDecoOverrideWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<CustomWatermarks>();

        playoutItem.Watermarks.Clear();
        playoutItem.Watermarks.AddRange(((CustomWatermarks)playoutItemWatermark).Watermarks);

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkDefaultDeco);
    }

    [Test]
    public void Should_Select_Playout_Item_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_With_Default_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = DefaultDecoInheritWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkPlayoutItem);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Global_And_Channel_Configured_With_Default_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = DefaultDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkChannel);
    }

    [Test]
    public void Should_Select_Global_Watermark_When_Global_Configured_With_Default_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelNoWatermark;
        var playoutItem = DefaultDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkGlobal);
    }

    [Test]
    public void Should_Select_No_Watermark_When_None_Configured_With_Default_Deco_Inherit()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkNone;
        var channel = ChannelNoWatermark;
        var playoutItem = DefaultDecoInherit;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public void Should_Select_Template_Deco_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_With_Template_Deco_Inherit_Default_Deco_Override()
    {
        Option<ChannelWatermark> globalWatermark = WatermarkGlobal;
        var channel = ChannelWithWatermark;
        var playoutItem = TemplateDecoInheritDefaultDecoOverrideWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<CustomWatermarks>();

        playoutItem.Watermarks.Clear();
        playoutItem.Watermarks.AddRange(((CustomWatermarks)playoutItemWatermark).Watermarks);

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(WatermarkDefaultDeco);
    }

    private static IEnumerable<(Option<ChannelWatermark>, Channel, PlayoutItem, List<ChannelWatermark>)>
        SelectWatermarksTestCases()
    {
        // STANDARD ----------------------------

        // no watermark when none are configured
        yield return (WatermarkNone, ChannelNoWatermark, PlayoutItemWithNoWatermarks, WatermarkResultEmpty);

        // no watermark when global configured but disabled playout item
        yield return (WatermarkGlobal, ChannelNoWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // global watermark when global configured
        yield return (WatermarkGlobal, ChannelNoWatermark, PlayoutItemWithNoWatermarks, [WatermarkGlobal.Head()]);

        // channel watermark when global and channel configured
        yield return (WatermarkGlobal, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [WatermarkChannel.Head()]);

        // channel watermark when channel configured
        yield return (WatermarkNone, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [WatermarkChannel.Head()]);

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

        // PLAYOUT TEMPLATE DECO --------------------------------

        // no watermark when global, channel and playout item configured with template deco disabled
        //yield return (WatermarkGlobal, ChannelWithWatermark, TemplateDecoDisableWithWatermark, WatermarkResultEmpty);

        // PLAYOUT DEFAULT DECO --------------------------------
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
