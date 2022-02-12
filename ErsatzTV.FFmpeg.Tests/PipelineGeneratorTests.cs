using System;
using System.Collections.Generic;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.PixelFormat;
using NUnit.Framework;
using FluentAssertions;

namespace ErsatzTV.FFmpeg.Tests;

[TestFixture]
public class PipelineGeneratorTests
{
    [Test]
    public void Correct_Codecs_And_Pixel_Format_Should_Copy()
    {
        var testFile = new InputFile(
            "/tmp/whatever.mkv",
            new List<MediaStream>
            {
                new VideoStream(0, VideoFormat.H264, new PixelFormatYuv420P()),
                new AudioStream(1, AudioFormat.Aac)
            });

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(VideoFormat.H264, new PixelFormatYuv420P(), AudioFormat.Aac);

        IList<IPipelineStep> result = PipelineGenerator.GeneratePipeline(inputFiles, desiredState);

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
                new AudioStream(1, AudioFormat.Aac)
            });

        var inputFiles = new List<InputFile> { testFile };

        var desiredState = new FrameState(VideoFormat.Hevc, new PixelFormatYuv420P(), AudioFormat.Aac);

        IList<IPipelineStep> result = PipelineGenerator.GeneratePipeline(inputFiles, desiredState);

        result.Should().Contain(p => p is EncoderLibx265);
        result.Should().Contain(p => p is EncoderCopyAudio);

        PrintCommand(inputFiles, result);
    }

    private static void PrintCommand(IEnumerable<InputFile> inputFiles, IList<IPipelineStep> pipeline)
    {
        string command = CommandGenerator.GenerateCommand(inputFiles, pipeline);
        Console.WriteLine($"Generated command: ffmpeg {command}");
    }
}
