using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class VaapiDriverHealthCheck : BaseHealthCheck, IVaapiDriverHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public VaapiDriverHealthCheck(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override string Title => "VAAPI Driver";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<FFmpegProfile> profiles = await dbContext.FFmpegProfiles
            .Filter(p => p.HardwareAcceleration == HardwareAccelerationKind.Vaapi)
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0)
        {
            return NotApplicableResult();
        }


        var defaultProfiles = profiles
            .Filter(p => p.VaapiDriver == VaapiDriver.Default)
            .ToList();

        return defaultProfiles.Any()
            ? InfoResult(
                $"{defaultProfiles.Count} FFmpeg Profile{(defaultProfiles.Count > 1 ? "s are" : " is")} set to use Default VAAPI Driver; selecting iHD (Gen 8+) or i965 (up to Gen 9) may offer better performance with Intel iGPU")
            : OkResult();
    }
}