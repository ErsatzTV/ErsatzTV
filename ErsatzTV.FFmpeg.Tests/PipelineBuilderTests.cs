using System;
using System.Collections.Generic;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.FFmpeg.State;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace ErsatzTV.FFmpeg.Tests;

[TestFixture]
public class PipelineGeneratorTests
{
    private readonly ILogger _logger = new Mock<ILogger>().Object;

    [Test]
    public void Incorrect_Video_Codec_Should_Use_Encoder()
    {
        var videoInputFile = new VideoInputFile(
            "/tmp/whatever.mkv",
            new List<VideoStream>
                { new(0, VideoFormat.H264, new PixelFormatYuv420P(), ColorParams.Default, new FrameSize(1920, 1080), "1:1", "16:9", "24", false) });

        var audioInputFile = new AudioInputFile(
            "/tmp/whatever.mkv",
            new List<AudioStream> { new(1, AudioFormat.Aac, 2) },
            new AudioState(
                AudioFormat.Aac,
                2,
                320,
                640,
                48,
                Option<TimeSpan>.None,
                false));

        var desiredState = new FrameState(
            true,
            false,
            VideoFormat.Hevc,
            new PixelFormatYuv420P(),
            new FrameSize(1920, 1080),
            new FrameSize(1920, 1080),
            false,
            Option<int>.None,
            2000,
            4000,
            90_000,
            false);

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            TimeSpan.FromSeconds(1),
            Option<TimeSpan>.None,
            false,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0,
            Option<int>.None,
            Option<int>.None);

        var builder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            videoInputFile,
            audioInputFile,
            None,
            None,
            "",
            "",
            _logger);
        FFmpegPipeline result = builder.Build(ffmpegState, desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderLibx265);

        string command = PrintCommand(videoInputFile, audioInputFile, None, None, result);
        command.Should().Be(
            "-threads 1 -nostdin -hide_banner -nostats -loglevel error -fflags +genpts+discardcorrupt+igndts -ss 00:00:01 -c:v h264 -re -i /tmp/whatever.mkv -map 0:1 -map 0:0 -muxdelay 0 -muxpreload 0 -movflags +faststart -flags cgop -sc_threshold 0 -video_track_timescale 90000 -b:v 2000k -maxrate:v 2000k -bufsize:v 4000k -c:a aac -b:a 320k -maxrate:a 320k -bufsize:a 640k -ar 48k -c:v libx265 -tag:v hvc1 -x265-params log-level=error -f mpegts -mpegts_flags +initial_discontinuity pipe:1");
    }

    [Test]
    public void Aac_6_Channel_Should_Specify_Audio_Channels()
    {
        var videoInputFile = new VideoInputFile(
            "/tmp/whatever.mkv",
            new List<VideoStream>
                { new(0, VideoFormat.H264, new PixelFormatYuv420P(), ColorParams.Default, new FrameSize(1920, 1080), "1:1", "16:9", "24", false) });

        var audioInputFile = new AudioInputFile(
            "/tmp/whatever.mkv",
            new List<AudioStream> { new(1, AudioFormat.Aac, 6) },
            new AudioState(
                AudioFormat.Aac,
                6,
                320,
                640,
                48,
                Option<TimeSpan>.None,
                false));

        var desiredState = new FrameState(
            true,
            false,
            VideoFormat.Hevc,
            new PixelFormatYuv420P(),
            new FrameSize(1920, 1080),
            new FrameSize(1920, 1080),
            false,
            Option<int>.None,
            2000,
            4000,
            90_000,
            false);

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            TimeSpan.FromSeconds(1),
            Option<TimeSpan>.None,
            false,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0,
            Option<int>.None,
            Option<int>.None);

        var builder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            videoInputFile,
            audioInputFile,
            None,
            None,
            "",
            "",
            _logger);
        FFmpegPipeline result = builder.Build(ffmpegState, desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderLibx265);

        string command = PrintCommand(videoInputFile, audioInputFile, None, None, result);
        command.Should().Be(
            "-threads 1 -nostdin -hide_banner -nostats -loglevel error -fflags +genpts+discardcorrupt+igndts -ss 00:00:01 -c:v h264 -re -i /tmp/whatever.mkv -map 0:1 -map 0:0 -muxdelay 0 -muxpreload 0 -movflags +faststart -flags cgop -sc_threshold 0 -video_track_timescale 90000 -b:v 2000k -maxrate:v 2000k -bufsize:v 4000k -c:a aac -ac 6 -b:a 320k -maxrate:a 320k -bufsize:a 640k -ar 48k -c:v libx265 -tag:v hvc1 -x265-params log-level=error -f mpegts -mpegts_flags +initial_discontinuity pipe:1");
    }

    [Test]
    public void Concat_Test()
    {
        var resolution = new FrameSize(1920, 1080);
        var concatInputFile = new ConcatInputFile("http://localhost:8080/ffmpeg/concat/1", resolution);

        var builder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            None,
            None,
            None,
            None,
            "",
            "",
            _logger);
        FFmpegPipeline result = builder.Concat(concatInputFile, FFmpegState.Concat(false, "Some Channel"));

        result.PipelineSteps.Should().HaveCountGreaterThan(0);

        string command = PrintCommand(None, None, None, concatInputFile, result);

        command.Should().Be(
            "-nostdin -hide_banner -nostats -loglevel error -fflags +genpts+discardcorrupt+igndts -f concat -safe 0 -protocol_whitelist file,http,tcp,https,tcp,tls -probesize 32 -re -stream_loop -1 -i http://localhost:8080/ffmpeg/concat/1 -muxdelay 0 -muxpreload 0 -movflags +faststart -flags cgop -sc_threshold 0 -c copy -map_metadata -1 -metadata service_provider=\"ErsatzTV\" -metadata service_name=\"Some Channel\" -f mpegts -mpegts_flags +initial_discontinuity pipe:1");
    }

    [Test]
    public void HlsDirect_Test()
    {
        var videoInputFile = new VideoInputFile(
            "/tmp/whatever.mkv",
            new List<VideoStream>
                { new(0, VideoFormat.H264, new PixelFormatYuv420P(), ColorParams.Default, new FrameSize(1920, 1080), "1:1", "16:9", "24", false) });

        var audioInputFile = new AudioInputFile(
            "/tmp/whatever.mkv",
            new List<AudioStream> { new(1, AudioFormat.Aac, 2) },
            new AudioState(
                AudioFormat.Copy,
                None,
                None,
                None,
                None,
                None,
                false));

        var desiredState = new FrameState(
            true,
            false,
            VideoFormat.Copy,
            Option<IPixelFormat>.None,
            new FrameSize(1920, 1080),
            new FrameSize(1920, 1080),
            false,
            Option<int>.None,
            2000,
            4000,
            90_000,
            false);

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            false,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0,
            Option<int>.None,
            Option<int>.None);

        var builder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            videoInputFile,
            audioInputFile,
            None,
            None,
            "",
            "",
            _logger);
        FFmpegPipeline result = builder.Build(ffmpegState, desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderCopyVideo);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderCopyAudio);

        string command = PrintCommand(videoInputFile, audioInputFile, None, None, result);

        command.Should().Be(
            "-nostdin -hide_banner -nostats -loglevel error -fflags +genpts+discardcorrupt+igndts -i /tmp/whatever.mkv -map 0:1 -map 0:0 -muxdelay 0 -muxpreload 0 -movflags +faststart -flags cgop -sc_threshold 0 -c:v copy -c:a copy -f mpegts -mpegts_flags +initial_discontinuity pipe:1");
    }

    [Test]
    public void HlsDirect_Test_All_Audio_Streams()
    {
        var videoInputFile = new VideoInputFile(
            "/tmp/whatever.mkv",
            new List<VideoStream>
                { new(0, VideoFormat.H264, new PixelFormatYuv420P(), ColorParams.Default, new FrameSize(1920, 1080), "1:1", "16:9", "24", false) });

        Option<AudioInputFile> audioInputFile = Option<AudioInputFile>.None;

        var desiredState = new FrameState(
            true,
            false,
            VideoFormat.Copy,
            new PixelFormatYuv420P(),
            new FrameSize(1920, 1080),
            new FrameSize(1920, 1080),
            false,
            Option<int>.None,
            2000,
            4000,
            90_000,
            false);

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            false,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0,
            Option<int>.None,
            Option<int>.None);

        var builder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            videoInputFile,
            audioInputFile,
            None,
            None,
            "",
            "",
            _logger);
        FFmpegPipeline result = builder.Build(ffmpegState, desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderCopyVideo);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderCopyAudio);

        string command = PrintCommand(videoInputFile, audioInputFile, None, None, result);

        command.Should().Be(
            "-nostdin -hide_banner -nostats -loglevel error -fflags +genpts+discardcorrupt+igndts -i /tmp/whatever.mkv -map 0:a -map 0:0 -muxdelay 0 -muxpreload 0 -movflags +faststart -flags cgop -sc_threshold 0 -c:v copy -c:a copy -f mpegts -mpegts_flags +initial_discontinuity pipe:1");
    }

    [Test]
    public void Resize_Image_Test()
    {
        var height = 200;

        var videoInputFile = new VideoInputFile(
            "/test/input/file.png",
            new List<VideoStream>
            {
                new(0, string.Empty, Option<IPixelFormat>.None, ColorParams.Default, FrameSize.Unknown, string.Empty, string.Empty, Option<string>.None, true)
            });

        var pipelineBuilder = new PipelineBuilder(
            new Mock<IRuntimeInfo>().Object,
            new DefaultHardwareCapabilities(),
            videoInputFile,
            Option<AudioInputFile>.None,
            Option<WatermarkInputFile>.None,
            Option<SubtitleInputFile>.None,
            "",
            "",
            _logger);

        FFmpegPipeline result = pipelineBuilder.Resize("/test/output/file.jpg", new FrameSize(-1, height));

        string command = PrintCommand(videoInputFile, None, None, None, result);

        command.Should().Be(
            "-nostdin -hide_banner -nostats -loglevel error -i /test/input/file.png -vf scale=-1:200:force_original_aspect_ratio=decrease /test/output/file.jpg");
    }

    private static string PrintCommand(
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<ConcatInputFile> concatInputFile,
        FFmpegPipeline pipeline)
    {
        IList<string> arguments = CommandGenerator.GenerateArguments(
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            concatInputFile,
            pipeline.PipelineSteps);

        var command = string.Join(" ", arguments);

        Console.WriteLine($"Generated command: ffmpeg {string.Join(" ", arguments)}");

        return command;
    }
}
