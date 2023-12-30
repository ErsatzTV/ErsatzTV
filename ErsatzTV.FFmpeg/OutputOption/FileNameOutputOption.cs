namespace ErsatzTV.FFmpeg.OutputOption;

public class FileNameOutputOption : OutputOption
{
    private readonly string _outputFile;

    public FileNameOutputOption(string outputFile) => _outputFile = outputFile;

    public override string[] OutputOptions => new[] { _outputFile };
}
