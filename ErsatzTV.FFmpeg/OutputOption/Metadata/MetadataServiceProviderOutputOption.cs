namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataServiceProviderOutputOption : OutputOption
{
    private readonly string _serviceProvider;

    public MetadataServiceProviderOutputOption(string serviceProvider) => _serviceProvider = serviceProvider;

    public override string[] OutputOptions => new[]
        { "-metadata", $"service_provider=\"{_serviceProvider}\"" };
}
