using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.GlobalOption;
using ErsatzTV.FFmpeg.InputOption;

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
        Option<GraphicsEngineInput> maybeGraphicsEngineInput,
        IList<IPipelineStep> pipelineSteps,
        bool isIntelVaapiOrQsv)
    {
        var arguments = new List<string>();

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.GlobalOptions);
        }

        bool isSameAudioAndVideo = maybeAudioInputFile.IsSome && maybeVideoInputFile.IsSome &&
                                   maybeAudioInputFile.Map(f => f.Path) == maybeVideoInputFile.Map(f => f.Path);

        var includedPaths = new System.Collections.Generic.HashSet<string>();
        foreach (VideoInputFile videoInputFile in maybeVideoInputFile)
        {
            includedPaths.Add(videoInputFile.Path);

            foreach (IInputOption step in videoInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(videoInputFile));
            }

            if (isSameAudioAndVideo)
            {
                foreach (AudioInputFile audioInputFile in maybeAudioInputFile)
                {
                    foreach (IDecoder decoder in audioInputFile.InputOptions.OfType<IDecoder>())
                    {
                        arguments.AddRange(decoder.InputOptions(audioInputFile));
                    }
                }
            }

            arguments.AddRange(["-i", videoInputFile.Path]);
        }

        foreach (AudioInputFile audioInputFile in maybeAudioInputFile)
        {
            if (!includedPaths.Contains(audioInputFile.Path) || isIntelVaapiOrQsv)
            {
                includedPaths.Add(audioInputFile.Path);

                foreach (IInputOption step in audioInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(audioInputFile));
                }

                arguments.AddRange(["-i", audioInputFile.Path]);
            }
        }

        foreach (WatermarkInputFile watermarkInputFile in maybeWatermarkInputFile)
        {
            if (includedPaths.Add(watermarkInputFile.Path))
            {
                foreach (IInputOption step in watermarkInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(watermarkInputFile));
                }

                arguments.AddRange(["-i", watermarkInputFile.Path]);
            }
        }

        foreach (ConcatInputFile concatInputFile in maybeConcatInputFile)
        {
            foreach (IInputOption step in concatInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(concatInputFile));
            }

            arguments.AddRange(["-i", concatInputFile.Path]);
        }

        foreach (GraphicsEngineInput graphicsEngineInput in maybeGraphicsEngineInput)
        {
            foreach (IInputOption step in graphicsEngineInput.InputOptions)
            {
                arguments.AddRange(step.InputOptions(graphicsEngineInput));
            }

            arguments.AddRange(["-i", graphicsEngineInput.Path]);
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
