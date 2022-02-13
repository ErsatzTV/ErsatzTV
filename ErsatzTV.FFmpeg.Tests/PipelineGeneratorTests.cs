using System;
using System.Collections.Generic;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Format;
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
    public void Correct_Codecs_And_Pixel_Format_Should_Copy()
    {
        var testFile = new InputFile(
            "/tmp/whatever.mkv",
            new List<MediaStream>
            {
                new VideoStream(0, VideoFormat.H264, new PixelFormatYuv420P()),
                new AudioStream(1, AudioFormat.Aac, 2)
            },
            TimeSpan.FromMinutes(2));

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(
            true,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            VideoFormat.H264,
            new PixelFormatYuv420P(),
            2000,
            4000,
            90_000,
            AudioFormat.Aac,
            2,
            320,
            640,
            48,
            Option<TimeSpan>.None);

        var builder = new PipelineBuilder(inputFiles, _logger);
        IList<IPipelineStep> result = builder.Build(desiredState);

        result.Should().Contain(p => p is EncoderCopyVideo);
        result.Should().Contain(p => p is EncoderCopyAudio);

        PrintCommand(inputFiles, result);
    }

    [Test]
    public void Incorrect_Video_Codec_Should_Use_Encoder()
    {
        var testFile = new InputFile(
            "/tmp/whatever.mkv",
            new List<MediaStream>
            {
                new VideoStream(0, VideoFormat.H264, new PixelFormatYuv420P()),
                new AudioStream(1, AudioFormat.Aac, 2)
            },
            TimeSpan.FromMinutes(2));

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(
            true,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            VideoFormat.Hevc,
            new PixelFormatYuv420P(),
            2000,
            4000,
            90_000,
            AudioFormat.Aac,
            2,
            320,
            640,
            48,
            Option<TimeSpan>.None);

        var builder = new PipelineBuilder(inputFiles, _logger);
        IList<IPipelineStep> result = builder.Build(desiredState);

        result.Should().Contain(p => p is EncoderLibx265);
        result.Should().Contain(p => p is EncoderAac);

        PrintCommand(inputFiles, result);
    }

    private static void PrintCommand(IEnumerable<InputFile> inputFiles, IList<IPipelineStep> pipeline)
    {
        IList<string> arguments = CommandGenerator.GenerateArguments(inputFiles, pipeline);
        Console.WriteLine($"Generated command: ffmpeg {string.Join(" ", arguments)}");
    }
}
