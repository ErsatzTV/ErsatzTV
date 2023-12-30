using System.Runtime.InteropServices;

namespace ErsatzTV.FFmpeg.Environment;

public class FFReportVariable : IPipelineStep
{
    private readonly Option<ConcatInputFile> _maybeConcatInputFile;
    private readonly string _reportsFolder;

    public FFReportVariable(string reportsFolder, Option<ConcatInputFile> maybeConcatInputFile)
    {
        _reportsFolder = reportsFolder;
        _maybeConcatInputFile = maybeConcatInputFile;
    }

    public EnvironmentVariable[] EnvironmentVariables
    {
        get
        {
            string fileName = _maybeConcatInputFile.IsSome
                ? Path.Combine(_reportsFolder, "ffmpeg-%t-concat.log")
                : Path.Combine(_reportsFolder, "ffmpeg-%t-transcode.log");

            // rework filename in a format that works on windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // \ is escape, so use / for directory separators
                fileName = fileName.Replace(@"\", @"/");

                // colon after drive letter needs to be escaped
                fileName = fileName.Replace(@":/", @"\:/");
            }

            return new[]
            {
                new EnvironmentVariable("FFREPORT", $"file={fileName}:level=32")
            };
        }
    }

    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
