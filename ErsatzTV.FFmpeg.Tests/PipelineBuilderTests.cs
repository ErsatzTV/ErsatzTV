using System;
using System.Collections.Generic;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using NUnit.Framework;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
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
                { new(0, VideoFormat.H264, new PixelFormatYuv420P(), new FrameSize(1920, 1080), "24", false) });

        var audioInputFile = new AudioInputFile(
            "/tmp/whatever.mkv",
            new List<AudioStream> { new(1, AudioFormat.Aac, 2) });

        var desiredState = new FrameState(
            false,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            true,
            false,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            VideoFormat.Hevc,
            new PixelFormatYuv420P(),
            new FrameSize(1920, 1080),
            new FrameSize(1920, 1080),
            Option<int>.None,
            2000,
            4000,
            90_000,
            false,
            AudioFormat.Aac,
            2,
            320,
            640,
            48,
            Option<TimeSpan>.None,
            false,
            false,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0);

        var builder = new PipelineBuilder(videoInputFile, audioInputFile, None, "", _logger);
        FFmpegPipeline result = builder.Build(desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);
        result.PipelineSteps.Should().Contain(ps => ps is EncoderLibx265);

        PrintCommand(videoInputFile, audioInputFile, None, result);
    }

    [Test]
    public void Concat_Test()
    {
        var resolution = new FrameSize(1920, 1080);
        var desiredState = FrameState.Concat(false, "Some Channel", resolution);

        var concatInputFile = new ConcatInputFile("http://localhost:8080/ffmpeg/concat/1", resolution);

        var builder = new PipelineBuilder(None, None, concatInputFile, "", _logger);
        FFmpegPipeline result = builder.Build(desiredState);

        result.PipelineSteps.Should().HaveCountGreaterThan(0);

        PrintCommand(None, None, concatInputFile, result);
    }

    private static void PrintCommand(
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<ConcatInputFile> concatInputFile,
        FFmpegPipeline pipeline)
    {
        IList<string> arguments = CommandGenerator.GenerateArguments(
            videoInputFile,
            audioInputFile,
            concatInputFile,
            pipeline.PipelineSteps);
        Console.WriteLine($"Generated command: ffmpeg {string.Join(" ", arguments)}");
    }
}
