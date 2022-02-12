namespace ErsatzTV.FFmpeg;

public static class CommandGenerator
{
    public static string GenerateCommand(IEnumerable<InputFile> inputFiles, IList<IPipelineStep> pipelineSteps)
    {
        var arguments = new List<string>();

        foreach (InputFile inputFile in inputFiles)
        {
            foreach (IPipelineStep step in pipelineSteps)
            {
                arguments.AddRange(step.InputOptions);
            }

            arguments.AddRange(new[] { "-i", inputFile.Path });
        }
        
        // TODO: complex filter

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.OutputOptions);
        }

        return string.Join(" ", arguments);
    }
}
