using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.Option.HardwareAcceleration;

namespace ErsatzTV.FFmpeg;

public static class CommandGenerator
{
    public static IList<EnvironmentVariable> GenerateEnvironmentVariables(IEnumerable<IPipelineStep> pipelineSteps) =>
        pipelineSteps.SelectMany(ps => ps.EnvironmentVariables).ToList();

    public static IList<string> GenerateArguments(
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<ConcatInputFile> maybeConcatInputFile,
        IList<IPipelineStep> pipelineSteps)
    {
        var arguments = new List<string>();

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.GlobalOptions);
        }

        var includedPaths = new System.Collections.Generic.HashSet<string>();
        foreach (VideoInputFile videoInputFile in maybeVideoInputFile)
        {
            includedPaths.Add(videoInputFile.Path);

            foreach (IInputOption step in videoInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(videoInputFile));
            }

            arguments.AddRange(new[] { "-i", videoInputFile.Path });
        }

        foreach (AudioInputFile audioInputFile in maybeAudioInputFile)
        {
            bool isVaapiOrQsv =
                pipelineSteps.Any(s => s is VaapiHardwareAccelerationOption or QsvHardwareAccelerationOption);
            
            if (!includedPaths.Contains(audioInputFile.Path) || isVaapiOrQsv)
            {
                includedPaths.Add(audioInputFile.Path);

                foreach (IInputOption step in audioInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(audioInputFile));
                }

                arguments.AddRange(new[] { "-i", audioInputFile.Path });
            }
        }

        foreach (WatermarkInputFile watermarkInputFile in maybeWatermarkInputFile)
        {
            if (!includedPaths.Contains(watermarkInputFile.Path))
            {
                includedPaths.Add(watermarkInputFile.Path);

                foreach (IInputOption step in watermarkInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(watermarkInputFile));
                }

                arguments.AddRange(new[] { "-i", watermarkInputFile.Path });
            }
        }

        foreach (ConcatInputFile concatInputFile in maybeConcatInputFile)
        {
            foreach (IInputOption step in concatInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(concatInputFile));
            }

            arguments.AddRange(new[] { "-i", concatInputFile.Path });
        }

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.FilterOptions);
        }

        // rearrange complex filter output options directly after video encoder
        var sortedSteps = pipelineSteps.Filter(s => s is not StreamSeekFilterOption && s is not ComplexFilter).ToList();
        Option<IPipelineStep> maybeComplex = pipelineSteps.Find(s => s is ComplexFilter);
        foreach (IPipelineStep complex in maybeComplex)
        {
            int encoderIndex = sortedSteps.FindIndex(s => s is EncoderBase { Kind: StreamKind.Video });
            sortedSteps.Insert(encoderIndex + 1, complex);
        }

        foreach (IPipelineStep step in sortedSteps)
        {
            
            arguments.AddRange(step.OutputOptions);
        }

        return arguments;
    }
}
