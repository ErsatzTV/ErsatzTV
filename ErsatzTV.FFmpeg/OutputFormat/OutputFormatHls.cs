using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatHls : IPipelineStep
{
    public const int SegmentSeconds = 4;

    private readonly FrameState _desiredState;
    private readonly bool _isFirstTranscode;
    private readonly Option<string> _mediaFrameRate;
    private readonly bool _oneSecondGop;
    private readonly bool _isTroubleshooting;
    private readonly string _playlistPath;
    private readonly string _segmentTemplate;

    public OutputFormatHls(
        FrameState desiredState,
        Option<string> mediaFrameRate,
        string segmentTemplate,
        string playlistPath,
        bool isFirstTranscode,
        bool oneSecondGop,
        bool isTroubleshooting)
    {
        _desiredState = desiredState;
        _mediaFrameRate = mediaFrameRate;
        _segmentTemplate = segmentTemplate;
        _playlistPath = playlistPath;
        _isFirstTranscode = isFirstTranscode;
        _oneSecondGop = oneSecondGop;
        _isTroubleshooting = isTroubleshooting;
    }

    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions => [];

    public string[] OutputOptions
    {
        get
        {
            int frameRate = _desiredState.FrameRate.IfNone(GetFrameRateFromMedia);

            int gop = _oneSecondGop ? frameRate : frameRate * SegmentSeconds;

            List<string> result =
            [
                "-g", $"{gop}",
                "-keyint_min", $"{frameRate * SegmentSeconds}",
                "-force_key_frames", $"expr:gte(t,n_forced*{SegmentSeconds})",
                "-f", "hls",
                "-hls_time", $"{SegmentSeconds}",
                "-hls_list_size", "0",
                "-segment_list_flags", "+live",
                "-hls_segment_filename",
                _segmentTemplate
            ];

            string pdt = _isTroubleshooting ? string.Empty : "program_date_time+omit_endlist+";

            if (_isFirstTranscode)
            {
                result.AddRange(
                [
                    "-hls_flags", $"{pdt}append_list+independent_segments",
                    _playlistPath
                ]);
            }
            else
            {
                result.AddRange(
                [
                    "-hls_flags", $"{pdt}append_list+discont_start+independent_segments",
                    "-mpegts_flags", "+initial_discontinuity",
                    _playlistPath
                ]);
            }

            return result.ToArray();
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
