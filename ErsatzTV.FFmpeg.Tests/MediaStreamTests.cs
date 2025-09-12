using ErsatzTV.FFmpeg.Format;
using LanguageExt;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.FFmpeg.Tests;

[TestFixture]
public class MediaStreamTests
{
    [Test]
    [SetCulture("it-IT")]
    public void SAR_0_0_DAR_4_3_Should_Not_Use_Comma_it_IT()
    {
        var mediaStream = new VideoStream(
            0,
            "h264",
            "main",
            Option<IPixelFormat>.None,
            ColorParams.Default,
            FrameSize.Unknown,
            "0:0",
            "4:3",
            Option<string>.None,
            false,
            ScanKind.Progressive);

        mediaStream.SampleAspectRatio.ShouldBe("1.333333333333:1");
    }

    [Test]
    [SetCulture("en-US")]
    public void SAR_0_0_DAR_4_3_Should_Not_Use_Comma_en_US()
    {
        var mediaStream = new VideoStream(
            0,
            "h264",
            "main",
            Option<IPixelFormat>.None,
            ColorParams.Default,
            FrameSize.Unknown,
            "0:0",
            "4:3",
            Option<string>.None,
            false,
            ScanKind.Progressive);

        mediaStream.SampleAspectRatio.ShouldBe("1.333333333333:1");
    }

    [Test]
    [SetCulture("en-US")]
    public void SAR_1_1_DAR_16_9_Should_Not_Use_Comma_en_US()
    {
        var mediaStream = new VideoStream(
            0,
            "h264",
            "main",
            Option<IPixelFormat>.None,
            ColorParams.Default,
            FrameSize.Unknown,
            "1:1",
            "16:9",
            Option<string>.None,
            false,
            ScanKind.Progressive);

        mediaStream.SampleAspectRatio.ShouldBe("1:1");
    }

    [Test]
    [SetCulture("en-US")]
    public void SAR_32_27_DAR_16_9_Should_Not_Use_Comma_en_US()
    {
        var mediaStream = new VideoStream(
            0,
            "h264",
            "main",
            Option<IPixelFormat>.None,
            ColorParams.Default,
            FrameSize.Unknown,
            "32:27",
            "16:9",
            Option<string>.None,
            false,
            ScanKind.Progressive);

        mediaStream.SampleAspectRatio.ShouldBe("32:27");
    }

    [Test]
    [SetCulture("en-US")]
    public void SAR_1point5_3point5_DAR_16_9_Should_Not_Use_Comma_en_US()
    {
        var mediaStream = new VideoStream(
            0,
            "h264",
            "main",
            Option<IPixelFormat>.None,
            ColorParams.Default,
            FrameSize.Unknown,
            "1.5:3.5",
            "16:9",
            Option<string>.None,
            false,
            ScanKind.Progressive);

        mediaStream.SampleAspectRatio.ShouldBe("1.5:3.5");
    }
}
