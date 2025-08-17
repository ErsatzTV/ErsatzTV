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
    private static readonly ChannelWatermark GlobalWatermark;
    private static readonly ChannelWatermark ChannelWatermark;
    private static readonly ChannelWatermark PlayoutItemWatermark;
    private static readonly Channel ChannelWithWatermark;
    private static readonly Channel ChannelNoWatermark;

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
        ChannelWatermark = new ChannelWatermark { Id = 1, Name = "Channel", Image = "ChannelImage" };
        PlayoutItemWatermark = new ChannelWatermark { Id = 2, Name = "PlayoutItem", Image = "PlayoutItemImage" };

        ChannelWithWatermark = new Channel(Guid.Empty)
            { Id = 0, Watermark = ChannelWatermark, WatermarkId = ChannelWatermark.Id };

        ChannelNoWatermark = new Channel(Guid.Empty) { Id = 0, Watermark = null, WatermarkId = null };
    }

    [Test]
    public async Task Should_Select_No_Watermark_When_None_Are_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = Option<ChannelWatermark>.None;
        var channel = ChannelNoWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public async Task Should_Select_No_Watermark_When_Global_Configured_But_Disabled_PlayoutItem()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelNoWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = true
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through
        await Task.Delay(10);

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark,
        //     mediaVersion,
        //     Option<ChannelWatermark>.None,
        //     Option<string>.None);
        //
        // watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public async Task Should_Select_Global_Watermark_When_Global_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelNoWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(GlobalWatermark);
    }

    [Test]
    public async Task Should_Select_Channel_Watermark_When_Global_And_Channel_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(ChannelWatermark);
    }

    [Test]
    public async Task Should_Select_Channel_Watermark_When_Channel_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = Option<ChannelWatermark>.None;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(ChannelWatermark);
    }

    [Test]
    public async Task Should_Select_No_Watermark_When_Channel_Configured_But_Disabled_PlayoutItem()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = Option<ChannelWatermark>.None;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [],
            DisableWatermarks = true
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        // GetWatermarkOptions is not even called when disableWatermarks is passed through
        await Task.Delay(10);

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark,
        //     mediaVersion,
        //     Option<ChannelWatermark>.None,
        //     Option<string>.None);
        //
        // watermarkOptions.ShouldBe(WatermarkOptions.NoWatermark);
    }

    [Test]
    public async Task Should_Select_PlayoutItem_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [PlayoutItemWatermark],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public async Task Should_Select_PlayoutItem_Watermark_When_Channel_And_PlayoutItem_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = Option<ChannelWatermark>.None;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [PlayoutItemWatermark],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public async Task Should_Select_PlayoutItem_Watermark_When_PlayoutItem_Configured()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = Option<ChannelWatermark>.None;
        var channel = ChannelNoWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [PlayoutItemWatermark],
            DisableWatermarks = false
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<InheritWatermark>();

        WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
            channel,
            playoutItem.Watermarks.HeadOrNone(),
            globalWatermark,
            mediaVersion,
            Option<ChannelWatermark>.None,
            Option<string>.None);

        watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }

    [Test]
    public async Task Should_Select_No_Watermark_When_Global_And_Channel_And_PlayoutItem_Configured_But_Disabled_PlayoutItem()
    {
        var now = DateTimeOffset.Now;
        Option<ChannelWatermark> globalWatermark = GlobalWatermark;
        var channel = ChannelWithWatermark;
        var playoutItem = new PlayoutItem
        {
            Watermarks = [PlayoutItemWatermark],
            DisableWatermarks = true
        };
        var mediaVersion = new MediaVersion();

        WatermarkResult playoutItemWatermark = WatermarkSelector.GetPlayoutItemWatermark(playoutItem, now);
        playoutItemWatermark.ShouldBeOfType<DisableWatermark>();

        await Task.Delay(10);

        // GetWatermarkOptions is not even called when disableWatermarks is passed through

        // WatermarkOptions watermarkOptions = await WatermarkSelector.GetWatermarkOptions(
        //     channel,
        //     playoutItem.Watermarks.HeadOrNone(),
        //     globalWatermark,
        //     mediaVersion,
        //     Option<ChannelWatermark>.None,
        //     Option<string>.None);
        //
        // watermarkOptions.ShouldNotBe(WatermarkOptions.NoWatermark);
        // watermarkOptions.Watermark.ShouldBe(PlayoutItemWatermark);
    }


    // TODO: test watermark override (used by song generation)

    // TODO: also decos?
}
