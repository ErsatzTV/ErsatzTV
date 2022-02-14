using System;
using System.Collections.Generic;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using NUnit.Framework;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;

namespace ErsatzTV.FFmpeg.Tests;

[TestFixture]
public class PipelineGeneratorTests
{
    private readonly ILogger _logger = new Mock<ILogger>().Object; 
    
    [Test]
    [Ignore("These aren't useful yet")]
    public void Correct_Codecs_And_Pixel_Format_Should_Copy()
    {
        var testFile = new InputFile(
            "/tmp/whatever.mkv",
            new List<MediaStream>
            {
                new VideoStream(0, VideoFormat.H264, new PixelFormatYuv420P(), new FrameSize(1920, 1080), "24"),
                new AudioStream(1, AudioFormat.Aac, 2)
            });

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(
            HardwareAccelerationMode.None,
            true,
            false,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            VideoFormat.H264,
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

        var builder = new PipelineBuilder(inputFiles, _logger);
        IList<IPipelineStep> result = builder.Build(desiredState);

        result.Should().HaveCountGreaterThan(0);

        PrintCommand(inputFiles, result);
    }

    [Test]
    [Ignore("These aren't useful yet")]
    public void Incorrect_Video_Codec_Should_Use_Encoder()
    {
        var testFile = new InputFile(
            "/tmp/whatever.mkv",
            new List<MediaStream>
            {
                new VideoStream(0, VideoFormat.H264, new PixelFormatYuv420P(), new FrameSize(1920, 1080), "24"),
                new AudioStream(1, AudioFormat.Aac, 2)
            });

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(
            HardwareAccelerationMode.None,
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

        var builder = new PipelineBuilder(inputFiles, _logger);
        IList<IPipelineStep> result = builder.Build(desiredState);

        result.Should().HaveCountGreaterThan(0);

        PrintCommand(inputFiles, result);
    }

    [Test]
    public void Concat_Test()
    {
        var resolution = new FrameSize(1920, 1080);
        var desiredState = FrameState.Concat("Some Channel", resolution);

        var inputFiles = new List<InputFile>
        {
            new ConcatInputFile("http://localhost:8080/ffmpeg/concat/1", resolution)
        };

        var builder = new PipelineBuilder(inputFiles, _logger);
        IList<IPipelineStep> result = builder.Build(desiredState);

        result.Should().HaveCountGreaterThan(0);

        PrintCommand(inputFiles, result);
    }

    private static void PrintCommand(IEnumerable<InputFile> inputFiles, IList<IPipelineStep> pipeline)
    {
        IList<string> arguments = CommandGenerator.GenerateArguments(inputFiles, pipeline);
        Console.WriteLine($"Generated command: ffmpeg {string.Join(" ", arguments)}");
    }
}
