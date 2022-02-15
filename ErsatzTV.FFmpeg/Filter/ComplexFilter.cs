using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly IList<InputFile> _inputFiles;
    private readonly IList<IPipelineFilterStep> _audioFilters;
    private readonly IList<IPipelineFilterStep> _videoFilters;

    public ComplexFilter(IList<InputFile> inputFiles, IList<IPipelineFilterStep> audioFilters, IList<IPipelineFilterStep> videoFilters)
    {
        _inputFiles = inputFiles;
        _audioFilters = audioFilters;
        _videoFilters = videoFilters;
    }

    private IList<string> Arguments()
    {
        var audioLabel = "0:a";
        var videoLabel = "0:v";

        var result = new List<string>();

        string audioFilterComplex = string.Empty;
        string videoFilterComplex = string.Empty;
        
        for (var i = 0; i < _inputFiles.Count; i++)
        {
            InputFile file = _inputFiles[i];
            for (var j = 0; j < file.Streams.Count; j++)
            {
                MediaStream stream = file.Streams[j];
                switch (stream.Kind)
                {
                    case StreamKind.Audio:
                        audioLabel = $"{i}:{stream.Index}";
                        if (_audioFilters.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                        {
                            audioFilterComplex += $"[{i}:{stream.Index}]";
                            audioFilterComplex += string.Join(
                                ",",
                                _audioFilters.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                            audioLabel = "[a]";
                            audioFilterComplex += audioLabel;
                        }
                        break;
                    case StreamKind.Video:
                        videoLabel = $"{i}:{stream.Index}";
                        if (_videoFilters.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                        {
                            videoFilterComplex += $"[{i}:{stream.Index}]";
                            videoFilterComplex += string.Join(
                                ",",
                                _videoFilters.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                            videoLabel = "[v]";
                            videoFilterComplex += videoLabel;
                        }
                        break;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(audioFilterComplex) || !string.IsNullOrWhiteSpace(videoFilterComplex))
        {
            var filterComplex = string.Join(
                ";",
                new[] { audioFilterComplex, videoFilterComplex }.Where(s => !string.IsNullOrWhiteSpace(s)));

            result.AddRange(new[] { "-filter_complex", filterComplex });
        }

        result.AddRange(new[] { "-map", audioLabel, "-map", videoLabel });

        return result;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Arguments();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
