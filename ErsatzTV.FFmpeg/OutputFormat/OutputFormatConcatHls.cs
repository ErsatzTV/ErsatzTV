using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatConcatHls : IPipelineStep
{
    private readonly string _playlistPath;
    private readonly string _segmentTemplate;

    public OutputFormatConcatHls(string segmentTemplate, string playlistPath)
    {
        _segmentTemplate = segmentTemplate;
        _playlistPath = playlistPath;
    }

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();

    public string[] OutputOptions
    {
        get
        {
            const int SEGMENT_SECONDS = 4;

            return
            [
                "-f", "hls",
                "-hls_time", $"{SEGMENT_SECONDS}",
                "-hls_list_size", "0",
                "-segment_list_flags", "+live",
                "-hls_segment_filename", _segmentTemplate,
                "-hls_flags", "program_date_time+append_list+omit_endlist+independent_segments",
                _playlistPath
            ];
        }
    }

    public FrameState NextState(FrameState currentState) => currentState;
}
