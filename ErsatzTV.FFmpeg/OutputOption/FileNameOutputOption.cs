namespace ErsatzTV.FFmpeg.OutputOption;

public class FileNameOutputOption : OutputOption
{
    private readonly string _outputFile;

    public FileNameOutputOption(string outputFile) => _outputFile = outputFile;

    public override IList<string> OutputOptions => new List<string> { _outputFile };
}
