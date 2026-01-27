using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatHls : IPipelineStep
{
    public const int SegmentSeconds = 4;

    private readonly FrameState _desiredState;
    private readonly bool _isFirstTranscode;
    private readonly bool _isTroubleshooting;
    private readonly Option<FrameRate> _mediaFrameRate;
    private readonly OutputFormatKind _outputFormat;
    private readonly Option<string> _segmentOptions;
    private readonly bool _oneSecondGop;
    private readonly string _playlistPath;
    private readonly string _segmentTemplate;
    private readonly Option<string> _initTemplate;

    public OutputFormatHls(
        FrameState desiredState,
        Option<FrameRate> mediaFrameRate,
        OutputFormatKind outputFormat,
        Option<string> segmentOptions,
        string segmentTemplate,
        Option<string> initTemplate,
        string playlistPath,
        bool isFirstTranscode,
        bool oneSecondGop,
        bool isTroubleshooting)
    {
        _desiredState = desiredState;
        _mediaFrameRate = mediaFrameRate;
        _outputFormat = outputFormat;
        _segmentOptions = segmentOptions;
        _segmentTemplate = segmentTemplate;
        _initTemplate = initTemplate;
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
            FrameRate frameRate = _desiredState.FrameRate.IfNone(_mediaFrameRate.IfNone(FrameRate.DefaultFrameRate));

            int gop = _oneSecondGop
                ? (int)Math.Round(frameRate.ParsedFrameRate)
                : (int)Math.Round(frameRate.ParsedFrameRate * SegmentSeconds);

            List<string> result = [];

            if (_desiredState.VideoFormat != VideoFormat.Copy)
            {
                result.AddRange(
                [
                    "-g", $"{gop}",
                    "-keyint_min", $"{(int)Math.Round(frameRate.ParsedFrameRate * SegmentSeconds)}",
                    "-force_key_frames", $"expr:gte(t,n_forced*{SegmentSeconds})"
                ]);
            }

            result.AddRange(
            [
                "-f", "hls",
                "-hls_time", $"{SegmentSeconds}",
                "-hls_list_size", "0",
                "-segment_list_flags", "+live",
                "-hls_segment_filename",
                _segmentTemplate
            ]);

            var independentSegments = "+independent_segments";

            switch (_outputFormat)
            {
                case OutputFormatKind.Hls:
                    result.AddRange(
                    [
                        "-hls_segment_type", "mpegts"
                    ]);
                    break;
                case OutputFormatKind.HlsMp4:
                    result.AddRange(
                    [
                        "-hls_segment_type", "fmp4",
                        "-hls_fmp4_init_filename", _initTemplate.IfNone($"{DateTimeOffset.Now.ToUnixTimeSeconds()}_init.mp4")
                    ]);
                    break;
            }

            foreach (string options in _segmentOptions)
            {
                result.AddRange("-hls_segment_options", options);
            }

            string pdt = _isTroubleshooting ? string.Empty : "program_date_time+omit_endlist+";

            if (_isFirstTranscode)
            {
                result.AddRange(
                [
                    "-hls_flags", $"{pdt}append_list{independentSegments}",
                    _playlistPath
                ]);
            }
            else
            {
                switch (_outputFormat)
                {
                    case  OutputFormatKind.HlsMp4:
                        result.AddRange(
                        [
                            "-hls_flags", $"{pdt}append_list+discont_start{independentSegments}",
                            _playlistPath
                        ]);
                        break;
                    default:
                        result.AddRange(
                        [
                            "-hls_flags", $"{pdt}append_list+discont_start{independentSegments}",
                            _playlistPath
                        ]);
                        break;
                }
            }

            return result.ToArray();
        }
    }

    public FrameState NextState(FrameState currentState) => currentState;
}
