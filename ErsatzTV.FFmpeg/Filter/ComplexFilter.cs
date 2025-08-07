using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Pipeline;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly Option<GraphicsEngineInput> _maybeGraphicsEngineInput;
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly PipelineContext _pipelineContext;

    public ComplexFilter(
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile,
        Option<GraphicsEngineInput> maybeGraphicsEngineInput,
        PipelineContext pipelineContext,
        FilterChain filterChain)
    {
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
        _maybeGraphicsEngineInput = maybeGraphicsEngineInput;
        _pipelineContext = pipelineContext;
        FilterChain = filterChain;

        FilterOptions = Arguments();
    }

    // for testing
    public FilterChain FilterChain { get; }

    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions { get; }

    public string[] OutputOptions => [];

    public FrameState NextState(FrameState currentState) => currentState;

    private string[] Arguments()
    {
        var audioLabel = "0:a";
        var videoLabel = "0:v";
        string? watermarkLabel = null;
        string? subtitleLabel = null;
        string? graphicsEngineLabel = null;

        var result = new List<string>();

        string audioFilterComplex = string.Empty;
        string videoFilterComplex = string.Empty;
        string watermarkFilterComplex = string.Empty;
        string watermarkOverlayFilterComplex = string.Empty;
        string subtitleFilterComplex = string.Empty;
        string subtitleOverlayFilterComplex = string.Empty;
        string graphicsEngineFilterComplex = string.Empty;
        string graphicsEngineOverlayFilterComplex = string.Empty;
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
            if (!distinctPaths.Contains(path) ||
                // use audio as a separate input with intel vaapi/qsv
                _pipelineContext.IsIntelVaapiOrQsv)
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

        foreach ((string path, _) in _maybeSubtitleInputFile.Filter(s => s.IsImageBased))
        {
            if (!distinctPaths.Contains(path))
            {
                distinctPaths.Add(path);
            }
        }

        foreach ((string path, _) in _maybeGraphicsEngineInput)
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
                if (FilterChain.VideoFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    videoFilterComplex += $"[{inputIndex}:{index}]";
                    videoFilterComplex += string.Join(
                        ",",
                        FilterChain.VideoFilterSteps.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
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
                if (FilterChain.WatermarkFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    watermarkFilterComplex += $"[{inputIndex}:{index}]";
                    watermarkFilterComplex += string.Join(
                        ",",
                        FilterChain.WatermarkFilterSteps.Select(f => f.Filter)
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

        foreach (SubtitleInputFile subtitleInputFile in _maybeSubtitleInputFile.Filter(s =>
                     s is { IsImageBased: true, Method: SubtitleMethod.Burn }))
        {
            int inputIndex = distinctPaths.IndexOf(subtitleInputFile.Path);
            foreach ((int index, _, _) in subtitleInputFile.Streams)
            {
                subtitleLabel = $"{inputIndex}:{index}";
                if (FilterChain.SubtitleFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    subtitleFilterComplex += $"[{inputIndex}:{index}]";
                    subtitleFilterComplex += string.Join(
                        ",",
                        FilterChain.SubtitleFilterSteps.Select(f => f.Filter)
                            .Filter(s => !string.IsNullOrWhiteSpace(s)));
                    subtitleLabel = "[st]";
                    subtitleFilterComplex += subtitleLabel;
                }
                else
                {
                    subtitleLabel = $"[{subtitleLabel}]";
                }
            }
        }

        foreach (GraphicsEngineInput graphicsEngineInput in _maybeGraphicsEngineInput)
        {
            int inputIndex = distinctPaths.IndexOf(graphicsEngineInput.Path);
            graphicsEngineLabel = $"{inputIndex}:0";
            if (FilterChain.GraphicsEngineFilterSteps.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
            {
                graphicsEngineFilterComplex += $"[{inputIndex}:0]";
                graphicsEngineFilterComplex += string.Join(
                    ",",
                    FilterChain.GraphicsEngineFilterSteps.Select(f => f.Filter)
                        .Filter(s => !string.IsNullOrWhiteSpace(s)));
                graphicsEngineLabel = "[ge]";
                graphicsEngineFilterComplex += graphicsEngineLabel;
            }
            else
            {
                graphicsEngineLabel = $"[{graphicsEngineLabel}]";
            }
        }

        // overlay subtitle
        if (!string.IsNullOrWhiteSpace(subtitleLabel) && FilterChain.SubtitleOverlayFilterSteps.Count != 0)
        {
            subtitleOverlayFilterComplex += $"{ProperLabel(videoLabel)}{ProperLabel(subtitleLabel)}";
            subtitleOverlayFilterComplex += string.Join(
                ",",
                FilterChain.SubtitleOverlayFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vst]";
            subtitleOverlayFilterComplex += videoLabel;
        }

        // overlay watermark
        if (!string.IsNullOrWhiteSpace(watermarkLabel) && FilterChain.WatermarkOverlayFilterSteps.Count != 0)
        {
            watermarkOverlayFilterComplex += $"{ProperLabel(videoLabel)}{ProperLabel(watermarkLabel)}";
            watermarkOverlayFilterComplex += string.Join(
                ",",
                FilterChain.WatermarkOverlayFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vwm]";
            watermarkOverlayFilterComplex += videoLabel;
        }

        // overlay graphics engine
        if (!string.IsNullOrWhiteSpace(graphicsEngineLabel) && FilterChain.GraphicsEngineOverlayFilterSteps.Count != 0)
        {
            graphicsEngineOverlayFilterComplex += $"{ProperLabel(videoLabel)}{ProperLabel(graphicsEngineLabel)}";
            graphicsEngineOverlayFilterComplex += string.Join(
                ",",
                FilterChain.GraphicsEngineOverlayFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vge]";
            graphicsEngineOverlayFilterComplex += videoLabel;
        }

        // pixel format
        if (FilterChain.PixelFormatFilterSteps.Count != 0)
        {
            pixelFormatFilterComplex += $"{ProperLabel(videoLabel)}";
            pixelFormatFilterComplex += string.Join(
                ",",
                FilterChain.PixelFormatFilterSteps.Select(f => f.Filter)
                    .Filter(s => !string.IsNullOrWhiteSpace(s)));
            videoLabel = "[vpf]";
            pixelFormatFilterComplex += videoLabel;
        }

        foreach (AudioInputFile audioInputFile in _maybeAudioInputFile)
        {
            int inputIndex = distinctPaths.LastIndexOf(audioInputFile.Path);
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
                graphicsEngineFilterComplex,
                subtitleOverlayFilterComplex,
                watermarkOverlayFilterComplex,
                graphicsEngineOverlayFilterComplex,
                pixelFormatFilterComplex
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(filterComplex))
        {
            result.AddRange(["-filter_complex", filterComplex]);
        }

        result.AddRange(["-map", videoLabel, "-map", audioLabel]);

        foreach (SubtitleInputFile subtitleInputFile in _maybeSubtitleInputFile.Filter(s =>
                     s.Method == SubtitleMethod.Copy ||
                     s is
                     {
                         IsImageBased: true, Method: SubtitleMethod.Convert
                     })) // TODO: support converting text subtitles?
        {
            if (subtitleInputFile.Streams.Any())
            {
                int inputIndex = distinctPaths.IndexOf(subtitleInputFile.Path);
                foreach ((int index, _, _) in subtitleInputFile.Streams)
                {
                    subtitleLabel = $"{inputIndex}:{index}";
                    result.AddRange(["-map", subtitleLabel]);
                }
            }
        }

        return result.ToArray();
    }

    private static string ProperLabel(string label) =>
        label.StartsWith("[", StringComparison.OrdinalIgnoreCase) ? label : $"[{label}]";
}
