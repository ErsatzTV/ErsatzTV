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
            var segmentType = "mpegts";

            // check for fmp4 output
            if (_segmentTemplate.Contains("m4s"))
            {
                segmentType = "fmp4";
            }

            return
            [
                "-g", $"{OutputFormatHls.SegmentSeconds}/2",
                "-force_key_frames", $"expr:gte(t,n_forced*{OutputFormatHls.SegmentSeconds}/2)",
                "-f", "hls",
                "-hls_segment_type", segmentType,
                //"-hls_init_time", "2",
                //"-hls_playlist_type", "event",
                "-hls_time", $"{OutputFormatHls.SegmentSeconds}",
                "-hls_list_size", "25", // burst of 45 means ~12 segments, so allow that plus a handful
                "-segment_list_flags", "+live",
                "-hls_segment_filename", _segmentTemplate,
                "-hls_flags", "delete_segments+program_date_time+omit_endlist+discont_start+independent_segments",
                "-master_pl_name", "playlist.m3u8",
                _playlistPath
            ];
        }
    }

    public FrameState NextState(FrameState currentState) => currentState;
}
