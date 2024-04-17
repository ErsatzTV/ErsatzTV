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
                $"Old config data exists; to migrate: exit ETV, backup the folder {FileSystemLayout.AppDataFolder} to another location, and restart ETV. Otherwise, move the old folder {FileSystemLayout.MacOsOldAppDataFolder} to another location to remove this message";
            return FailResult(message).AsTask();
        }

        return NotApplicableResult().AsTask();
    }
}
