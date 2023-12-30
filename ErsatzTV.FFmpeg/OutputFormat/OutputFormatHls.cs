using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatHls : IPipelineStep
{
    private readonly FrameState _desiredState;
    private readonly Option<string> _mediaFrameRate;
    private readonly string _playlistPath;
    private readonly bool _oneSecondGop;
    private readonly string _segmentTemplate;

    public OutputFormatHls(
        FrameState desiredState,
        Option<string> mediaFrameRate,
        string segmentTemplate,
        string playlistPath,
        bool oneSecondGop = false)
    {
        _desiredState = desiredState;
        _mediaFrameRate = mediaFrameRate;
        _segmentTemplate = segmentTemplate;
        _playlistPath = playlistPath;
        _oneSecondGop = oneSecondGop;
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
            int frameRate = _desiredState.FrameRate.IfNone(GetFrameRateFromMedia);

            int gop = _oneSecondGop ? frameRate : frameRate * SEGMENT_SECONDS;
            
            return new[]
            {
                "-g", $"{gop}",
                "-keyint_min", $"{frameRate * SEGMENT_SECONDS}",
                "-force_key_frames", $"expr:gte(t,n_forced*{SEGMENT_SECONDS})",
                "-f", "hls",
                "-hls_time", $"{SEGMENT_SECONDS}",
                "-hls_list_size", "0",
                "-segment_list_flags", "+live",
                "-hls_segment_filename",
                _segmentTemplate,
                "-hls_flags", "program_date_time+append_list+discont_start+omit_endlist+independent_segments",
                "-mpegts_flags", "+initial_discontinuity",
                _playlistPath
            };
        }
    }

    public FrameState NextState(FrameState currentState) => currentState;

    private int GetFrameRateFromMedia()
    {
        var frameRate = 24;

        foreach (string rFrameRate in _mediaFrameRate)
        {
            if (!int.TryParse(rFrameRate, out int fr))
            {
                string[] split = (rFrameRate ?? string.Empty).Split("/");
                if (int.TryParse(split[0], out int left) && int.TryParse(split[1], out int right))
                {
                    fr = (int)Math.Round(left / (double)right);
                }
                else
                {
                    fr = 24;
                }
            }

            frameRate = fr;
        }

        return frameRate;
    }
}
