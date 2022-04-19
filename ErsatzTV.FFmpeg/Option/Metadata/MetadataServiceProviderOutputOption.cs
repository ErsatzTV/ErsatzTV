namespace ErsatzTV.FFmpeg.Option.Metadata;

public class MetadataServiceProviderOutputOption : OutputOption
{
    private readonly string _serviceProvider;

    public MetadataServiceProviderOutputOption(string serviceProvider) => _serviceProvider = serviceProvider;

    public override IList<string> OutputOptions => new List<string>
        { "-metadata", $"service_provider=\"{_serviceProvider}\"" };
}
