using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public class NewComplexFilter : IPipelineStep
{
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly FilterChain _filterChain;
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly List<string> _outputOptions;
    private readonly IList<string> _arguments;

    public NewComplexFilter(
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile,
        FilterChain filterChain)
    {
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
        _filterChain = filterChain;

        _outputOptions = new List<string>();

        _arguments = Arguments();
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => _arguments;
    public IList<string> OutputOptions => _outputOptions;

    public FrameState NextState(FrameState currentState) => currentState;

    // for testing
    public FilterChain FilterChain => _filterChain;

    private List<string> Arguments()
    {
        var audioLabel = "0:a";
        var videoLabel = "0:v";
        string? watermarkLabel = null;
        string? subtitleLabel = null;

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
                if (_filterChain.VideoFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    videoFilterComplex += $"[{inputIndex}:{index}]";
                    videoFilterComplex += string.Join(
                        ",",
                        _filterChain.VideoFilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    videoLabel = "[v]";
                    videoFilterComplex += videoLabel;
                }
            }
        }

        foreach (WatermarkInputFile watermarkInputFile in _maybeWatermarkInputFile)
        {
            int inputIndex = distinctPaths.IndexOf(watermarkInputFile.Path);
            foreach ((int index, _, _) in watermarkInputFile.Streams)
            {
                watermarkLabel = $"{inputIndex}:{index}";
                if (_filterChain.WatermarkFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    watermarkFilterComplex += $"[{inputIndex}:{index}]";
                    watermarkFilterComplex += string.Join(
                        ",",
                        _filterChain.WatermarkFilterSteps.Select(f => f.Filter)
                            .Filter(s => !string.IsNullOrWhiteSpace(s)));
                    watermarkLabel = "[wm]";
                    watermarkFilterComplex += watermarkLabel;
                }
                else
                {
                    watermarkLabel = $"[{watermarkLabel}]";
                }
            }
        }

        foreach (SubtitleInputFile subtitleInputFile in _maybeSubtitleInputFile.Filter(s => !s.Copy))
        {
            int inputIndex = distinctPaths.IndexOf(subtitleInputFile.Path);
            foreach ((int index, _, _) in subtitleInputFile.Streams)
            {
                subtitleLabel = $"{inputIndex}:{index}";
                if (_filterChain.SubtitleFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    subtitleFilterComplex += $"[{inputIndex}:{index}]";
                    subtitleFilterComplex += string.Join(
                        ",",
                        _filterChain.SubtitleFilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    subtitleLabel = "[st]";
                    subtitleFilterComplex += subtitleLabel;
                }
                else
                {
                    subtitleLabel = $"[{subtitleLabel}]";
                }
            }
        }
        
        // overlay subtitle
        if (!string.IsNullOrWhiteSpace(subtitleLabel) && _filterChain.SubtitleOverlayFilterSteps.Any())
        {
            subtitleOverlayFilterComplex += $"{ProperLabel(videoLabel)}{ProperLabel(subtitleLabel)}";
            subtitleOverlayFilterComplex += string.Join(
                ",",
                _filterChain.SubtitleOverlayFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vst]";
            subtitleOverlayFilterComplex += videoLabel;
        }

        // overlay watermark
        if (!string.IsNullOrWhiteSpace(watermarkLabel) && _filterChain.WatermarkOverlayFilterSteps.Any())
        {
            watermarkOverlayFilterComplex += $"{ProperLabel(videoLabel)}{ProperLabel(watermarkLabel)}";
            watermarkOverlayFilterComplex += string.Join(
                ",",
                _filterChain.WatermarkOverlayFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vwm]";
            watermarkOverlayFilterComplex += videoLabel;
        }
        
        // pixel format
        if (_filterChain.PixelFormatFilterSteps.Any())
        {
            pixelFormatFilterComplex += $"{ProperLabel(videoLabel)}";
            pixelFormatFilterComplex += string.Join(
                ",",
                _filterChain.PixelFormatFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vpf]";
            pixelFormatFilterComplex += videoLabel;
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

        var filterComplex = string.Join(
            ";",
            new[]
            {
                audioFilterComplex,
                videoFilterComplex,
                subtitleFilterComplex,
                watermarkFilterComplex,
                subtitleOverlayFilterComplex,
                watermarkOverlayFilterComplex,
                pixelFormatFilterComplex
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

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

    private string ProperLabel(string label) => label.StartsWith("[") ? label : $"[{label}]";
}
