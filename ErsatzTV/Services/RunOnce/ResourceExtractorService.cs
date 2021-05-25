using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using Microsoft.Extensions.Hosting;

namespace ErsatzTV.Services.RunOnce
{
    public class ResourceExtractorService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(FileSystemLayout.ResourcesCacheFolder))
            {
                Directory.CreateDirectory(FileSystemLayout.ResourcesCacheFolder);
            }

            Assembly assembly = typeof(ResourceExtractorService).GetTypeInfo().Assembly;
            
            await ExtractResource(assembly, "background.png", cancellationToken);
            await ExtractResource(assembly, "ErsatzTV.png", cancellationToken);
            await ExtractResource(assembly, "Roboto-Regular.ttf", cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task ExtractResource(Assembly assembly, string name, CancellationToken cancellationToken)
        {
            await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.{name}");
            if (resource != null)
            {
                await using FileStream fs = File.Create(
                    Path.Combine(FileSystemLayout.ResourcesCacheFolder, name));
                await resource.CopyToAsync(fs, cancellationToken);
            }
        }
    }
}
