using ErsatzTV.FFmpeg.Environment;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Filter;

public class ComplexFilter : IPipelineStep
{
    private readonly Option<VideoInputFile> _maybeVideoInputFile;
    private readonly Option<AudioInputFile> _maybeAudioInputFile;
    private readonly IList<IPipelineFilterStep> _audioFilters;
    private readonly IList<IPipelineFilterStep> _videoFilters;

    public ComplexFilter(
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        IList<IPipelineFilterStep> audioFilters,
        IList<IPipelineFilterStep> videoFilters)
    {
        _maybeVideoInputFile = maybeVideoInputFile;
        _maybeAudioInputFile = maybeAudioInputFile;
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
        
        // TODO: handle when audio input file and video input file have the same path

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

        foreach (VideoInputFile videoInputFile in _maybeVideoInputFile)
        {
            int inputIndex = distinctPaths.IndexOf(videoInputFile.Path);
            foreach ((int index, _, _) in videoInputFile.Streams)
            {
                videoLabel = $"{inputIndex}:{index}";
                if (_videoFilters.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    videoFilterComplex += $"[{inputIndex}:{index}]";
                    videoFilterComplex += string.Join(
                        ",",
                        _videoFilters.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
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
                if (_audioFilters.Any(f => !string.IsNullOrWhiteSpace(f.Filter)))
                {
                    audioFilterComplex += $"[{inputIndex}:{index}]";
                    audioFilterComplex += string.Join(
                        ",",
                        _audioFilters.Select(f => f.Filter).Filter(s => !string.IsNullOrWhiteSpace(s)));
                    audioLabel = "[a]";
                    audioFilterComplex += audioLabel;
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
    public IList<string> VideoInputOptions(VideoInputFile videoInputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Arguments();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
