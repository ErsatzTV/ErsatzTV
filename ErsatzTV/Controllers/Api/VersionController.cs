using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class VersionController
{
    private static readonly string Version;

    static VersionController() =>
        Version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

    [HttpGet("/api/version")]
    public string GetVersion() => Version;
}
