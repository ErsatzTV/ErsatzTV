namespace ErsatzTV.FFmpeg;

public static class CommandGenerator
{
    public static IList<string> GenerateArguments(
        IEnumerable<InputFile> inputFiles,
        IList<IPipelineStep> pipelineSteps)
    {
        var arguments = new List<string>();

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.GlobalOptions);
        }

        foreach (InputFile inputFile in inputFiles)
        {
            foreach (IPipelineStep step in pipelineSteps)
            {
                arguments.AddRange(step.InputOptions);
            }

            arguments.AddRange(new[] { "-i", inputFile.Path });
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
