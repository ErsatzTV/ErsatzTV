using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class VersionController
{
    private static readonly CombinedVersion Version;

    static VersionController() =>
        Version = new CombinedVersion(
            1,
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown");

    [HttpGet("/api/version", Name="GetVersion")]
    [Tags("Version")]
    [EndpointSummary("Get version")]
    public CombinedVersion GetVersion() => Version;

    public record CombinedVersion(int ApiVersion, string AppVersion);
}
