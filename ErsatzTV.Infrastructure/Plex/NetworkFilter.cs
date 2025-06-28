using Refit;

namespace ErsatzTV.Infrastructure.Plex;

public class NetworkFilter
{
    [AliasAs("network")]
    public string Network { get; set; }
}
