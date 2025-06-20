using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class VaapiDriverHealthCheck(
    IHardwareCapabilitiesFactory hardwareCapabilitiesFactory,
    IDbContextFactory<TvContext> dbContextFactory)
    : BaseHealthCheck, IVaapiDriverHealthCheck
{
    public override string Title => "VAAPI Driver";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Channel> channels = await dbContext.Channels
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var channelFFmpegProfiles = channels
            .Map(c => c.FFmpegProfileId)
            .ToImmutableHashSet();

        List<FFmpegProfile> ffmpegProfiles = await dbContext.FFmpegProfiles
            .AsNoTracking()
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken);

        var activeFFmpegProfiles = ffmpegProfiles
            .Filter(f => channelFFmpegProfiles.Contains(f.Id))
            .ToList();

        if (activeFFmpegProfiles.Count == 0)
        {
            return NotApplicableResult();
        }

        Option<string> maybeFFmpegPath = await dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath);
        if (maybeFFmpegPath.IsNone)
        {
            return NotApplicableResult();
        }

        foreach (string ffmpegPath in maybeFFmpegPath)
        {
            IFFmpegCapabilities ffmpegCapabilities = await hardwareCapabilitiesFactory.GetFFmpegCapabilities(ffmpegPath);
            foreach (FFmpegProfile profile in activeFFmpegProfiles)
            {
                Option<string> vaapiDriver = VaapiDriverName(profile.VaapiDriver);

                IHardwareCapabilities capabilities = await hardwareCapabilitiesFactory.GetHardwareCapabilities(
                    ffmpegCapabilities,
                    ffmpegPath,
                    HardwareAccelerationMode.Vaapi,
                    profile.VaapiDisplay,
                    vaapiDriver,
                    profile.VaapiDevice
                );

                if (capabilities is VaapiHardwareCapabilities { EntrypointCount: 0 } or NoHardwareCapabilities)
                {
                    return FailResult(
                        $"FFmpeg Profile {profile.Name} is using device and driver combination ({profile.VaapiDevice} and {profile.VaapiDriver}) that reports no capabilities. Hardware Acceleration WILL NOT WORK as configured.");
                }
            }
        }


        var defaultProfiles = activeFFmpegProfiles
            .Filter(p => p.VaapiDriver == VaapiDriver.Default)
            .ToList();

        return defaultProfiles.Count != 0
            ? InfoResult(
                $"{defaultProfiles.Count} FFmpeg Profile{(defaultProfiles.Count > 1 ? "s are" : " is")} set to use Default VAAPI Driver; selecting iHD (Gen 8+) or i965 (up to Gen 9) may offer better performance with Intel iGPU")
            : OkResult();
    }

    private static Option<string> VaapiDriverName(VaapiDriver driver) =>
        driver switch
        {
            VaapiDriver.i965 => "i965",
            VaapiDriver.iHD => "iHD",
            VaapiDriver.RadeonSI => "radeonsi",
            VaapiDriver.Nouveau => "nouveau",
            _ => Option<string>.None
        };
}
