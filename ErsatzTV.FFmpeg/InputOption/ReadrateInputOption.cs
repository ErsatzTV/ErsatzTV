using System.Globalization;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Environment;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.InputOption;

public class ReadrateInputOption : IInputOption
{
    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly int _initialBurstSeconds;
    private readonly ILogger _logger;

    public ReadrateInputOption(
        IFFmpegCapabilities ffmpegCapabilities,
        int initialBurstSeconds,
        ILogger logger)
    {
        _ffmpegCapabilities = ffmpegCapabilities;
        _initialBurstSeconds = initialBurstSeconds;
        _logger = logger;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();

    public IList<string> GlobalOptions => Array.Empty<string>();

    public IList<string> InputOptions(InputFile inputFile)
    {
        var result = new List<string> { "-readrate", "1.0" };

        if (_initialBurstSeconds > 0)
        {
            if (!_ffmpegCapabilities.HasOption(FFmpegKnownOption.ReadrateInitialBurst))
            {
                _logger.LogWarning(
                    "FFmpeg is missing {Option} option; unable to transcode faster than realtime",
                    FFmpegKnownOption.ReadrateInitialBurst.Name);

                return result;
            }

            result.AddRange(
                new[]
                {
                    "-readrate_initial_burst",
                    _initialBurstSeconds.ToString(CultureInfo.InvariantCulture)
                });
        }

        return result;
    }

    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState with { Realtime = true };

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    // don't use realtime input for a still image
    public bool AppliesTo(VideoInputFile videoInputFile) => videoInputFile.VideoStreams.All(s => !s.StillImage);

    public bool AppliesTo(ConcatInputFile concatInputFile) => true;
}
