using ErsatzTV.FFmpeg.Environment;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly FrameSize _resolution;

    public ComplexFilter(
        FrameState currentState,
        FFmpegState ffmpegState,
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        FrameSize resolution)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _resolution = resolution;
    }

    private IList<string> Arguments()
    {
        var audioLabel = "0:a";
        var videoLabel = "0:v";
        string watermarkLabel;

        var result = new List<string>();

        string audioFilterComplex = string.Empty;
        string videoFilterComplex = string.Empty;
        string watermarkFilterComplex = string.Empty;
        string overlayFilterComplex = string.Empty;
        
        var distinctPaths = new List<string>();
        foreach ((string path, _) in _maybeVideoInputFile)
        {
            if (!distinctPaths.Contains(path))
            {
                distinctPaths.Add(path);
            }
        }

        foreach ((string path, _) in _maybeAudioInputFile)
        {
            if (!distinctPaths.Contains(path))
            {
                distinctPaths.Add(path);
            }
        }

        foreach ((string path, _) in _maybeWatermarkInputFile)
        {
            if (!distinctPaths.Contains(path))
            {
                distinctPaths.Add(path);
            }
        }

        foreach (VideoInputFile videoInputFile in _maybeVideoInputFile)
        {
            int inputIndex = distinctPaths.IndexOf(videoInputFile.Path);
            foreach ((int index, _, _) in videoInputFile.Streams)
            {
                videoLabel = $"{inputIndex}:{index}";
                if (videoInputFile.FilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    videoFilterComplex += $"[{inputIndex}:{index}]";
                    videoFilterComplex += string.Join(
                        ",",
                        videoInputFile.FilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    videoLabel = "[v]";
                    videoFilterComplex += videoLabel;
                }
            }
        }

        foreach (AudioInputFile audioInputFile in _maybeAudioInputFile)
        {
            int inputIndex = distinctPaths.IndexOf(audioInputFile.Path);
            foreach ((int index, _, _) in audioInputFile.Streams)
            {
                audioLabel = $"{inputIndex}:{index}";
                if (audioInputFile.FilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    audioFilterComplex += $"[{inputIndex}:{index}]";
                    audioFilterComplex += string.Join(
                        ",",
                        audioInputFile.FilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    audioLabel = "[a]";
                    audioFilterComplex += audioLabel;
                }
            }
        }

        foreach (WatermarkInputFile watermarkInputFile in _maybeWatermarkInputFile)
        {
            int inputIndex = distinctPaths.IndexOf(watermarkInputFile.Path);
            foreach ((int index, _, _) in watermarkInputFile.Streams)
            {
                watermarkLabel = $"{inputIndex}:{index}";
                if (watermarkInputFile.FilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    watermarkFilterComplex += $"[{inputIndex}:{index}]";
                    watermarkFilterComplex += string.Join(
                        ",",
                        watermarkInputFile.FilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    watermarkLabel = "[wm]";
                    watermarkFilterComplex += watermarkLabel;
                }

                IPipelineFilterStep overlayFilter = AvailableOverlayFilters.ForAcceleration(
                    _ffmpegState.HardwareAccelerationMode,
                    _currentState,
                    watermarkInputFile.DesiredState,
                    _resolution);

                if (overlayFilter.Filter != string.Empty)
                {
                    string tempVideoLabel = string.IsNullOrWhiteSpace(videoFilterComplex)
                        ? $"[{videoLabel}]"
                        : videoLabel;

                    // vaapi uses software overlay and needs to upload
                    string uploadFilter = string.Empty;
                    if (_ffmpegState.HardwareAccelerationMode == HardwareAccelerationMode.Vaapi)
                    {
                        uploadFilter = new HardwareUploadFilter(_ffmpegState).Filter;
                    }

                    if (!string.IsNullOrWhiteSpace(uploadFilter))
                    {
                        uploadFilter = "," + uploadFilter;
                    }
                    
                    overlayFilterComplex = $"{tempVideoLabel}{watermarkLabel}{overlayFilter.Filter}{uploadFilter}[vf]";
                    
                    // change the mapped label
                    videoLabel = "[vf]";
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(audioFilterComplex) || !string.IsNullOrWhiteSpace(videoFilterComplex))
        {
            var filterComplex = string.Join(
                ";",
                new[] { audioFilterComplex, videoFilterComplex, watermarkFilterComplex, overlayFilterComplex }.Where(
                    s => !string.IsNullOrWhiteSpace(s)));

            result.AddRange(new[] { "-filter_complex", filterComplex });
        }

        result.AddRange(new[] { "-map", audioLabel, "-map", videoLabel });

        return result;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Arguments();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
