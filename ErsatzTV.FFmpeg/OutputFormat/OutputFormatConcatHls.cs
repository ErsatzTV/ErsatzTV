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

    public string[] OutputOptions =>
        [
        //"-g", $"{gop}",
        //"-keyint_min", $"{FRAME_RATE * OutputFormatHls.SegmentSeconds}",
        "-force_key_frames", $"expr:gte(t,n_forced*{OutputFormatHls.SegmentSeconds}/2)",
        "-f", "hls",
        //"-hls_init_time", "2",
        "-hls_time", $"{OutputFormatHls.SegmentSeconds}",
        "-hls_list_size", "55", // burst of 180 means 45 segments, so allow that plus 10
        "-segment_list_flags", "+live",
        "-hls_segment_filename", _segmentTemplate,
        "-hls_flags", "delete_segments+program_date_time+omit_endlist",
        _playlistPath
    ];

    public FrameState NextState(FrameState currentState) => currentState;
}
