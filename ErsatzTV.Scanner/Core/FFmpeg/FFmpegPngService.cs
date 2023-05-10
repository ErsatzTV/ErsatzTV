using CliWrap;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Scanner.Core.FFmpeg;

public class FFmpegPngService : IFFmpegPngService
{
    public Command ConvertToPng(string ffmpegPath, string inputFile, string outputFile)
    {
        string[] arguments =
        {
            "-threads", "1",
            "-nostdin",
            "-hide_banner", "-loglevel", "error", "-nostats",
            "-i", inputFile,
            "-f", "apng", "-y", outputFile
        };

        return Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
    }

    public Command ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile)
    {
        string[] arguments =
        {
            "-threads", "1",
            "-nostdin",
            "-hide_banner", "-loglevel", "error", "-nostats",
            "-i", inputFile,
            "-map", $"0:{streamIndex}",
            "-f", "apng", "-y", outputFile
        };

        return Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
    }
}
