using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class VersionController
{
    private static readonly CombinedVersion Version;

    static VersionController() =>
        Version = new CombinedVersion(
            3,
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown");

    [HttpGet("/api/version", Name="GetVersion")]
    [Tags("Version")]
    [EndpointSummary("Get version")]
    [EndpointGroupName("general")]
    public CombinedVersion GetVersion() => Version;

    public record CombinedVersion(int ApiVersion, string AppVersion);
}
