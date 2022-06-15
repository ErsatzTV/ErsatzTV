using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;
    private readonly string _fontsDir;
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly FrameSize _resolution;

    public ComplexFilter(
        FrameState currentState,
        FFmpegState ffmpegState,
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile,
        FrameSize resolution,
        string fontsDir)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
        _resolution = resolution;
        _fontsDir = fontsDir;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Arguments();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    private IList<string> Arguments()
    {
        var audioLabel = "0:a";
        var videoLabel = "0:v";
        string watermarkLabel;
        string subtitleLabel;

        var result = new List<string>();

        string audioFilterComplex = string.Empty;
        string videoFilterComplex = string.Empty;
        string watermarkFilterComplex = string.Empty;
        string watermarkOverlayFilterComplex = string.Empty;
        string subtitleFilterComplex = string.Empty;
        string subtitleOverlayFilterComplex = string.Empty;

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

        foreach ((string path, _) in _maybeSubtitleInputFile)
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
                        watermarkInputFile.FilterSteps.Select(f => f.Filter)
                            .Filter(s => !string.IsNullOrWhiteSpace(s)));
                    watermarkLabel = "[wm]";
                    watermarkFilterComplex += watermarkLabel;
                }
                else
                {
                    watermarkLabel = $"[{watermarkLabel}]";
                }

                IPipelineFilterStep overlayFilter = AvailableWatermarkOverlayFilters.ForAcceleration(
                    _ffmpegState.EncoderHardwareAccelerationMode,
                    _currentState,
                    watermarkInputFile.DesiredState,
                    _resolution);

                if (overlayFilter.Filter != string.Empty)
                {
                    string tempVideoLabel = string.IsNullOrWhiteSpace(videoFilterComplex)
                        ? $"[{videoLabel}]"
                        : videoLabel;

                    // vaapi uses software overlay and needs to upload
                    // videotoolbox seems to require a hwupload for hevc
                    // also wait to upload if a subtitle overlay is coming
                    string uploadDownloadFilter = string.Empty;
                    if (_maybeSubtitleInputFile.IsNone &&
                        (_ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi ||
                         _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.VideoToolbox &&
                         _currentState.VideoFormat == VideoFormat.Hevc))
                    {
                        uploadDownloadFilter = new HardwareUploadFilter(_ffmpegState).Filter;
                    }

                    if (_maybeSubtitleInputFile.Map(s => !s.IsImageBased).IfNone(false) &&
                        _ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.Vaapi &&
                        _ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.VideoToolbox)
                    {
                        uploadDownloadFilter = new HardwareDownloadFilter(_currentState).Filter;
                    }

                    if (!string.IsNullOrWhiteSpace(uploadDownloadFilter))
                    {
                        uploadDownloadFilter = "," + uploadDownloadFilter;
                    }

                    watermarkOverlayFilterComplex =
                        $"{tempVideoLabel}{watermarkLabel}{overlayFilter.Filter}{uploadDownloadFilter}[vf]";

                    // change the mapped label
                    videoLabel = "[vf]";
                }
            }
        }

        foreach (SubtitleInputFile subtitleInputFile in _maybeSubtitleInputFile.Filter(s => !s.Copy))
        {
            int inputIndex = distinctPaths.IndexOf(subtitleInputFile.Path);
            foreach ((int index, _, _) in subtitleInputFile.Streams)
            {
                subtitleLabel = $"{inputIndex}:{index}";
                if (subtitleInputFile.FilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    subtitleFilterComplex += $"[{inputIndex}:{index}]";
                    subtitleFilterComplex += string.Join(
                        ",",
                        subtitleInputFile.FilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    subtitleLabel = "[st]";
                    subtitleFilterComplex += subtitleLabel;
                }
                else
                {
                    subtitleLabel = $"[{subtitleLabel}]";
                }

                string filter;
                if (subtitleInputFile.IsImageBased)
                {
                    IPipelineFilterStep overlayFilter =
                        AvailableSubtitleOverlayFilters.ForAcceleration(
                            _ffmpegState.EncoderHardwareAccelerationMode,
                            _currentState);
                    filter = overlayFilter.Filter;
                }
                else
                {
                    subtitleLabel = string.Empty;
                    var subtitlesFilter = new SubtitlesFilter(_fontsDir, subtitleInputFile);
                    filter = subtitlesFilter.Filter;
                }

                if (filter != string.Empty)
                {
                    string tempVideoLabel = string.IsNullOrWhiteSpace(videoFilterComplex) &&
                                            string.IsNullOrWhiteSpace(watermarkFilterComplex)
                        ? $"[{videoLabel}]"
                        : videoLabel;

                    // vaapi uses software overlay and needs to upload
                    // videotoolbox seems to require a hwupload for hevc
                    string uploadFilter = string.Empty;
                    if (_ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi
                        || _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.VideoToolbox &&
                        _currentState.VideoFormat == VideoFormat.Hevc)
                    {
                        uploadFilter = new HardwareUploadFilter(_ffmpegState).Filter;
                    }

                    if (!string.IsNullOrWhiteSpace(uploadFilter))
                    {
                        uploadFilter = "," + uploadFilter;
                    }

                    subtitleOverlayFilterComplex = $"{tempVideoLabel}{subtitleLabel}{filter}{uploadFilter}[vst]";

                    // change the mapped label
                    videoLabel = "[vst]";
                }
            }
        }

        var filterComplex = string.Join(
            ";",
            new[]
            {
                audioFilterComplex,
                videoFilterComplex,
                watermarkFilterComplex,
                subtitleFilterComplex,
                watermarkOverlayFilterComplex,
                subtitleOverlayFilterComplex
            }.Where(
                s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(filterComplex))
        {
            result.AddRange(new[] { "-filter_complex", filterComplex });
        }

        result.AddRange(new[] { "-map", audioLabel, "-map", videoLabel });

        foreach (SubtitleInputFile subtitleInputFile in _maybeSubtitleInputFile.Filter(s => s.Copy))
        {
            int inputIndex = distinctPaths.IndexOf(subtitleInputFile.Path);
            foreach ((int index, _, _) in subtitleInputFile.Streams)
            {
                subtitleLabel = $"{inputIndex}:{index}";
                result.AddRange(new[] { "-map", subtitleLabel });
            }
        }

        return result;
    }
}
