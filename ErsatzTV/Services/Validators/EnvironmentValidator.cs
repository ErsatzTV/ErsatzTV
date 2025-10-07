using ErsatzTV.Core;

namespace ErsatzTV.Services.Validators;

public class EnvironmentValidator(ILogger<EnvironmentValidator> logger)
    : IEnvironmentValidator
{
    private const long OneHundredTwentyEightMegabytes = 128000000;

    public Task<bool> Validate()
    {
        long configFreeSpace = long.MaxValue;
        long transcodeFreeSpace = long.MaxValue;

        try
        {
            var configDriveInfo = new DriveInfo(FileSystemLayout.AppDataFolder);
            configFreeSpace = configDriveInfo.AvailableFreeSpace;

            var transcodeDriveInfo = new DriveInfo(FileSystemLayout.TranscodeFolder);
            transcodeFreeSpace = transcodeDriveInfo.AvailableFreeSpace;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to validate available free space");
        }

        if (configFreeSpace < OneHundredTwentyEightMegabytes)
        {
            logger.LogCritical(
                "ErsatzTV requires at least 128 MB of free space at {ConfigFolder}",
                FileSystemLayout.AppDataFolder);

            return Task.FromResult(false);
        }

        if (transcodeFreeSpace < OneHundredTwentyEightMegabytes)
        { 
            logger.LogCritical(
                "ErsatzTV requires at least 128 MB of free space at {TranscodeFolder}",
                FileSystemLayout.TranscodeFolder);

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
