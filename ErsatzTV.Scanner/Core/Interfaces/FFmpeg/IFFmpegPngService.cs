using CliWrap;

namespace ErsatzTV.Scanner.Core.Interfaces.FFmpeg;

public interface IFFmpegPngService
{
    Command ConvertToPng(string ffmpegPath, string inputFile, string outputFile);

    Command ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile);
}
