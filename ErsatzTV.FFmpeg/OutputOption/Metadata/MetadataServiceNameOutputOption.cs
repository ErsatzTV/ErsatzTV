namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataServiceNameOutputOption : OutputOption
{
    private readonly string _serviceName;

    public MetadataServiceNameOutputOption(string serviceName) => _serviceName = serviceName;

    public override string[] OutputOptions => new[]
        { "-metadata", $"service_name=\"{_serviceName}\"" };
}
