using System.Runtime.InteropServices;
using ErsatzTV.Core;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class MacOsConfigFolderHealthCheck : BaseHealthCheck, IMacOsConfigFolderHealthCheck
{
    public override string Title => "MacOS Config Folder";

    public Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        // only applies to macos
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return NotApplicableResult().AsTask();
        }

        if (Directory.Exists(FileSystemLayout.MacOsOldAppDataFolder))
        {
            var message =
                $"Old config data exists; to migrate: exit ETV, delete the folder {FileSystemLayout.AppDataFolder} and restart ETV";
            return WarningResult(message).AsTask();
        }

        return OkResult().AsTask();
    }
}
