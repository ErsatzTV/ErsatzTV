using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class VersionController
{
    private static readonly string Version;

    static VersionController() =>
        Version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

    [HttpGet("/api/version")]
    [Tags("Version")]
    [EndpointSummary("Get version")]
    public string GetVersion() => Version;
}
