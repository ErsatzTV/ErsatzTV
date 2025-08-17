using ErsatzTV.Core.Domain;
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
    private static readonly Option<ChannelWatermark> GlobalWatermark;
    private static readonly Option<ChannelWatermark> NoGlobalWatermark;
    private static readonly Option<ChannelWatermark> ChannelWatermark;
    private static readonly ChannelWatermark PlayoutItemWatermark;
    private static readonly Channel ChannelWithWatermark;
    private static readonly Channel ChannelNoWatermark;
    private static readonly DateTimeOffset Now = new(2025, 08, 17, 12, 0, 0, TimeSpan.FromHours(-5));

    private static readonly PlayoutItem PlayoutItemDisableWatermarks =
        new() { Watermarks = [], DisableWatermarks = true };

    private static readonly PlayoutItem PlayoutItemWithNoWatermarks =
        new() { Watermarks = [], DisableWatermarks = false };

    private static readonly PlayoutItem PlayoutItemWithWatermark;

    private static readonly PlayoutItem PlayoutItemWithDisabledWatermark;

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

        GlobalWatermark = new ChannelWatermark { Id = 0, Name = "Global", Image = "GlobalImage" };
        NoGlobalWatermark = Option<ChannelWatermark>.None;

        ChannelWatermark = new ChannelWatermark { Id = 1, Name = "Channel", Image = "ChannelImage" };
        PlayoutItemWatermark = new ChannelWatermark { Id = 2, Name = "PlayoutItem", Image = "PlayoutItemImage" };

        foreach (var channelWatermark in ChannelWatermark)
        {
            ChannelWithWatermark = new Channel(Guid.Empty)
                { Id = 0, Watermark = channelWatermark, WatermarkId = channelWatermark.Id };
        }

        ChannelNoWatermark = new Channel(Guid.Empty) { Id = 0, Watermark = null, WatermarkId = null };

        PlayoutItemWithWatermark = new PlayoutItem { Watermarks = [PlayoutItemWatermark], DisableWatermarks = false };

        PlayoutItemWithDisabledWatermark = new PlayoutItem
            { Watermarks = [PlayoutItemWatermark], DisableWatermarks = true };
    }

    [Test]
    public void Should_Select_No_Watermark_When_None_Are_Configured()
    {
        Option<ChannelWatermark> globalWatermark = NoGlobalWatermark;
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
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
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
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(GlobalWatermark);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Global_And_Channel_Configured()
    {
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(ChannelWatermark);
    }

    [Test]
    public void Should_Select_Channel_Watermark_When_Channel_Configured()
    {
        Option<ChannelWatermark> globalWatermark = NoGlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithNoWatermarks;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(ChannelWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Channel_Configured_But_Disabled_PlayoutItem()
    {
        Option<ChannelWatermark> globalWatermark = NoGlobalWatermark;
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
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_PlayoutItem_Watermark_When_Channel_And_PlayoutItem_Configured()
    {
        Option<ChannelWatermark> globalWatermark = NoGlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_PlayoutItem_Watermark_When_PlayoutItem_Configured()
    {
        Option<ChannelWatermark> globalWatermark = NoGlobalWatermark;
        var channel = ChannelNoWatermark;
        var playoutItem = PlayoutItemWithWatermark;

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, Now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public void Should_Select_No_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_But_Disabled_PlayoutItem()
    {
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
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

    private static IEnumerable<(Option<ChannelWatermark>, Channel, PlayoutItem, List<ChannelWatermark>)>
        SelectWatermarksTestCases()
    {
        // no watermark when none are configured
        yield return (NoGlobalWatermark, ChannelNoWatermark, PlayoutItemWithNoWatermarks, WatermarkResultEmpty);

        // no watermark when global configured but disabled playout item
        yield return (GlobalWatermark, ChannelNoWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // global watermark when global configured
        yield return (GlobalWatermark, ChannelNoWatermark, PlayoutItemWithNoWatermarks, [GlobalWatermark.Head()]);

        // channel watermark when global and channel configured
        yield return (GlobalWatermark, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [ChannelWatermark.Head()]);

        // channel watermark when channel configured
        yield return (NoGlobalWatermark, ChannelWithWatermark, PlayoutItemWithNoWatermarks, [ChannelWatermark.Head()]);

        // playout item when global, channel and playout item configured
        yield return (GlobalWatermark, ChannelWithWatermark, PlayoutItemWithWatermark, [PlayoutItemWatermark]);

        // playout item when channel and playout item configured
        yield return (NoGlobalWatermark, ChannelWithWatermark, PlayoutItemWithWatermark, [PlayoutItemWatermark]);

        // playout item when playout item configured
        yield return (NoGlobalWatermark, ChannelNoWatermark, PlayoutItemWithWatermark, [PlayoutItemWatermark]);

        // no watermark when channel configured with playout item disabled
        yield return (NoGlobalWatermark, ChannelWithWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // no watermark when global and channel configured with playout item disabled
        yield return (GlobalWatermark, ChannelWithWatermark, PlayoutItemDisableWatermarks, WatermarkResultEmpty);

        // no watermark when global, channel and playout item configured with playout item disabled
        yield return (GlobalWatermark, ChannelWithWatermark, PlayoutItemWithDisabledWatermark, WatermarkResultEmpty);
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
