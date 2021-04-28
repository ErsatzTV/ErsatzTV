using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services
{
    public class JellyfinService : BackgroundService
    {
        private readonly ILogger<JellyfinService> _logger;

        public JellyfinService(ILogger<JellyfinService> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(FileSystemLayout.JellyfinSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.JellyfinSecretsPath, "{}", cancellationToken);
            }

            _logger.LogInformation(
                "Jellyfin service started; secrets are at {JellyfinSecretsPath}",
                FileSystemLayout.JellyfinSecretsPath);
        }
    }
}
