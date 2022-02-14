namespace ErsatzTV.FFmpeg.Option.Metadata;

public class MetadataServiceNameOutputOption : OutputOption
{
    private readonly string _serviceName;

    public MetadataServiceNameOutputOption(string serviceName)
    {
        _serviceName = serviceName;
    }

    public override IList<string> OutputOptions => new List<string>
        { "-metadata", $"service_name=\"{_serviceName}\"" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        MetadataServiceName = _serviceName
    };
}
