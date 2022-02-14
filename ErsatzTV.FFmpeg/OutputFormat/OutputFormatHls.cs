using LanguageExt;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatHls : IPipelineStep
{
    private readonly FrameState _desiredState;
    private readonly Option<string> _mediaFrameRate;
    private readonly string _segmentTemplate;
    private readonly string _playlistPath;

    public OutputFormatHls(
        FrameState desiredState,
        Option<string> mediaFrameRate,
        string segmentTemplate,
        string playlistPath)
    {
        _desiredState = desiredState;
        _mediaFrameRate = mediaFrameRate;
        _segmentTemplate = segmentTemplate;
        _playlistPath = playlistPath;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();

    public IList<string> OutputOptions
    {
        get
        {
            const int SEGMENT_SECONDS = 4;
            int frameRate = _desiredState.FrameRate.IfNone(GetFrameRateFromMedia);

            return new List<string>
            {
                "-g", $"{frameRate * SEGMENT_SECONDS}",
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

    public FrameState NextState(FrameState currentState) => currentState with
    {
        OutputFormat = OutputFormatKind.Hls,
        HlsPlaylistPath = _playlistPath,
        HlsSegmentTemplate = _segmentTemplate
    };
}
