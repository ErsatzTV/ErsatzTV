using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;
    private readonly string _fontsDir;
    private readonly ILogger _logger;
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly Option<IPixelFormat> _desiredPixelFormat;
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly FrameSize _resolution;
    private readonly List<string> _outputOptions;
    private readonly List<IPipelineStep> _pipelineSteps;
    private readonly IList<string> _arguments;

    public ComplexFilter(
        FrameState currentState,
        FFmpegState ffmpegState,
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile,
        Option<IPixelFormat> desiredPixelFormat,
        FrameSize resolution,
        string fontsDir,
        ILogger logger)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
        _desiredPixelFormat = desiredPixelFormat;
        _resolution = resolution;
        _fontsDir = fontsDir;
        _logger = logger;

        _outputOptions = new List<string>();
        _pipelineSteps = new List<IPipelineStep>();

        _arguments = Arguments();
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => _arguments;
    public IList<string> OutputOptions => _outputOptions;
    public IList<IPipelineStep> PipelineSteps => _pipelineSteps;

    public FrameState NextState(FrameState currentState) => currentState;

    private List<string> Arguments()
    {
        var state = _currentState;
        
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
        string pixelFormatFilterComplex = string.Empty;

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

                foreach (VideoInputFile videoInputFile in _maybeVideoInputFile)
                foreach (VideoStream stream in videoInputFile.VideoStreams)
                {
                    IPipelineFilterStep overlayFilter = AvailableWatermarkOverlayFilters.ForAcceleration(
                        _ffmpegState.EncoderHardwareAccelerationMode,
                        watermarkInputFile.DesiredState,
                        _resolution,
                        stream.SquarePixelFrameSize(_resolution),
                        _logger);

                    if (overlayFilter.Filter != string.Empty)
                    {
                        _pipelineSteps.Add(overlayFilter);
                        
                        state = overlayFilter.NextState(state);

                        string tempVideoLabel = string.IsNullOrWhiteSpace(videoFilterComplex)
                            ? $"[{videoLabel}]"
                            : videoLabel;

                        // vaapi uses software overlay and needs to upload
                        // videotoolbox seems to require a hwupload for hevc
                        // also wait to upload if a subtitle overlay is coming
                        string uploadDownloadFilter = string.Empty;
                        if (_maybeSubtitleInputFile.IsNone &&
                            (_ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi ||
                             _ffmpegState.EncoderHardwareAccelerationMode ==
                             HardwareAccelerationMode.VideoToolbox &&
                             state.VideoFormat == VideoFormat.Hevc))
                        {
                            var hardwareUpload = new HardwareUploadFilter(_ffmpegState);
                            _pipelineSteps.Add(hardwareUpload);
                            uploadDownloadFilter = hardwareUpload.Filter;
                            state = state with { FrameDataLocation = FrameDataLocation.Hardware };
                        }

                        if (_maybeSubtitleInputFile.Map(s => !s.IsImageBased).IfNone(false) &&
                            _ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.Vaapi &&
                            _ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.VideoToolbox &&
                            _ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.Amf)
                        {
                            var hardwareDownload = new HardwareDownloadFilter(state);
                            _pipelineSteps.Add(hardwareDownload);
                            uploadDownloadFilter = hardwareDownload.Filter;
                            state = state with { FrameDataLocation = FrameDataLocation.Software };
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
                    IPipelineFilterStep overlayFilter = AvailableSubtitleOverlayFilters.ForAcceleration(
                        _ffmpegState.EncoderHardwareAccelerationMode);
                    _pipelineSteps.Add(overlayFilter);
                    state = overlayFilter.NextState(state);
                    filter = overlayFilter.Filter;
                }
                else
                {
                    subtitleLabel = string.Empty;
                    var subtitlesFilter = new SubtitlesFilter(_fontsDir, subtitleInputFile);
                    _pipelineSteps.Add(subtitlesFilter);
                    state = subtitlesFilter.NextState(state);
                    filter = subtitlesFilter.Filter;
                }

                if (filter != string.Empty)
                {
                    string tempVideoLabel = videoLabel.StartsWith('[') && videoLabel.EndsWith(']')
                        ? videoLabel
                        : $"[{videoLabel}]";

                    // vaapi uses software overlay and needs to upload
                    // videotoolbox seems to require a hwupload for hevc
                    string uploadFilter = string.Empty;
                    if (_ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi
                        || _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.VideoToolbox &&
                        state.VideoFormat == VideoFormat.Hevc)
                    {
                        var hardwareUpload = new HardwareUploadFilter(_ffmpegState);
                        _pipelineSteps.Add(hardwareUpload);
                        uploadFilter = hardwareUpload.Filter;
                        if (!string.IsNullOrWhiteSpace(uploadFilter))
                        {
                            state = state with { FrameDataLocation = FrameDataLocation.Hardware };
                        }
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

        foreach (VideoStream videoStream in _maybeVideoInputFile.Map(vif => vif.VideoStreams).Flatten())
        foreach (IPixelFormat pixelFormat in _desiredPixelFormat)
        {
            _logger.LogDebug("Desired pixel format {PixelFormat}", pixelFormat);
            
            string tempVideoLabel = videoLabel.StartsWith("[") && videoLabel.EndsWith("]")
                ? videoLabel
                : $"[{videoLabel}]";

            // normalize pixel format and color params
            string filter = string.Empty;

            if (!videoStream.ColorParams.IsBt709)
            {
                _logger.LogDebug("Adding colorspace filter");
                var colorspace = new ColorspaceFilter(_currentState, videoStream, pixelFormat);
                _pipelineSteps.Add(colorspace);
                filter = colorspace.Filter;
            }

            if (state.PixelFormat.Map(f => f.FFmpegName) != pixelFormat.FFmpegName)
            {
                _logger.LogDebug(
                    "Format {A} doesn't equal {B}",
                    state.PixelFormat.Map(f => f.FFmpegName),
                    pixelFormat.FFmpegName);

                if (videoStream.ColorParams.IsHdr)
                {
                    _logger.LogWarning("HDR tone mapping is not implemented; colors may appear incorrect");
                    continue;
                }

                if (state.FrameDataLocation == FrameDataLocation.Software)
                {
                    _logger.LogDebug("Frame data location is SOFTWARE");
                    _outputOptions.AddRange(new[] { "-pix_fmt",  pixelFormat.FFmpegName });
                }
                else
                {
                    _logger.LogDebug("Frame data location is HARDWARE");
                    filter = _ffmpegState.EncoderHardwareAccelerationMode switch
                    {
                        HardwareAccelerationMode.Nvenc => $"{filter},scale_cuda=format={pixelFormat.FFmpegName}",
                        _ => filter
                    };
                }
            }
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                pixelFormatFilterComplex = $"{tempVideoLabel}{filter}[vpf]";

                // change the mapped label
                videoLabel = "[vpf]";
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
                subtitleOverlayFilterComplex,
                pixelFormatFilterComplex
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
