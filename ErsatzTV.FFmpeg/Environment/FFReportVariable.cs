using System.Runtime.InteropServices;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Environment;

public class FFReportVariable : IPipelineStep
{
    private readonly string _reportsFolder;
    private readonly Option<ConcatInputFile> _maybeConcatInputFile;

    public FFReportVariable(string reportsFolder, Option<ConcatInputFile> maybeConcatInputFile)
    {
        _reportsFolder = reportsFolder;
        _maybeConcatInputFile = maybeConcatInputFile;
    }

    public IList<EnvironmentVariable> EnvironmentVariables
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

            return new List<EnvironmentVariable>
            {
                new("FFREPORT", $"file={fileName}:level=32")
            };
        }
    }

    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
