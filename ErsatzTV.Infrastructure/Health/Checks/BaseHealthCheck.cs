using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core.Health;
using LanguageExt;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Health.Checks
{
    public abstract class BaseHealthCheck
    {
        protected abstract string Title { get; }

        protected HealthCheckResult Result(HealthCheckStatus status, string message) =>
            new(Title, status, message);

        protected HealthCheckResult NotApplicableResult() =>
            new(Title, HealthCheckStatus.NotApplicable, string.Empty);
        
        protected HealthCheckResult OkResult() =>
            new(Title, HealthCheckStatus.Pass, string.Empty);

        protected HealthCheckResult FailResult(string message) =>
            new(Title, HealthCheckStatus.Fail, message);

        protected HealthCheckResult WarningResult(string message) =>
            new(Title, HealthCheckStatus.Warning, message);

        protected HealthCheckResult InfoResult(string message) =>
            new(Title, HealthCheckStatus.Info, message);
        
        protected static async Task<string> GetProcessOutput(string path, IEnumerable<string> arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.AddRange(arguments);

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            string result = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return result;
        }
    }
}
