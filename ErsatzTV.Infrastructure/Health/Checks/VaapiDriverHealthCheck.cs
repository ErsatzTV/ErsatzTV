using System;
using System.Reflection;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Health.Checks
{
    public class VaapiDriverHealthCheck : BaseHealthCheck, IVaapiDriverHealthCheck
    {
        private readonly IConfigElementRepository _configElementRepository;

        public VaapiDriverHealthCheck(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        protected override string Title => "VAAPI Driver";

        public async Task<HealthCheckResult> Check()
        {
            string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";

            if (!version.Contains("docker", StringComparison.OrdinalIgnoreCase) ||
                !version.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
            {
                return NotApplicableResult();
            }
            
            Option<int> maybeVaapiDriver =
                await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegVaapiDriver);
            var vaapiDriver = (VaapiDriver)await maybeVaapiDriver.IfNoneAsync(0);
            if (vaapiDriver == VaapiDriver.Default)
            {
                return InfoResult(
                    "Settings > FFmpeg Settings > VAAPI Driver is set to Default; selecting iHD (Gen 8+) or i965 (up to Gen 9) may offer better performance");
            }

            return OkResult();
        }
    }
}
