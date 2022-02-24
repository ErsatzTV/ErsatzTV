using ErsatzTV.FFmpeg.Environment;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public static class CommandGenerator
{
    public static IList<EnvironmentVariable> GenerateEnvironmentVariables(IEnumerable<IPipelineStep> pipelineSteps)
    {
        return pipelineSteps.SelectMany(ps => ps.EnvironmentVariables).ToList();
    }

    public static IList<string> GenerateArguments(
        IEnumerable<VideoInputFile> videoInputFiles,
        IEnumerable<AudioInputFile> audioInputFiles,
        Option<ConcatInputFile> concatInputFile,
        IList<IPipelineStep> pipelineSteps)
    {
        // TODO: handle when audio input file and video input file have the same path
        
        var arguments = new List<string>();

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.GlobalOptions);
        }

        var includedPaths = new System.Collections.Generic.HashSet<string>();
        foreach (VideoInputFile videoInputFile in videoInputFiles)
        {
            includedPaths.Add(videoInputFile.Path);
            
            foreach (IPipelineStep step in pipelineSteps)
            {
                arguments.AddRange(step.VideoInputOptions(videoInputFile));
            }

            arguments.AddRange(new[] { "-i", videoInputFile.Path });
        }
        
        foreach ((string path, _) in audioInputFiles)
        {
            if (!includedPaths.Contains(path))
            {
                includedPaths.Add(path);
                arguments.AddRange(new[] { "-i", path });
            }
        }

        foreach (ConcatInputFile concat in concatInputFile)
        {
            foreach (IPipelineStep step in pipelineSteps)
            {
                // TODO: this is kind of messy
                arguments.AddRange(
                    step.VideoInputOptions(
                        new VideoInputFile(
                            string.Empty,
                            Array.Empty<VideoStream>())));
            }

            arguments.AddRange(new[] { "-i", concat.Path });
        }

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.FilterOptions);
        }

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.OutputOptions);
        }

        return arguments;
    }
}
