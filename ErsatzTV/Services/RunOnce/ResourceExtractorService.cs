using System.Reflection;
using ErsatzTV.Core;

namespace ErsatzTV.Services.RunOnce;

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
        await ExtractResource(assembly, "song_background_1.png", cancellationToken);
        await ExtractResource(assembly, "song_background_2.png", cancellationToken);
        await ExtractResource(assembly, "song_background_3.png", cancellationToken);
        await ExtractResource(assembly, "ErsatzTV.png", cancellationToken);
        await ExtractResource(assembly, "Roboto-Regular.ttf", cancellationToken);
        await ExtractResource(assembly, "OPTIKabel-Heavy.otf", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
