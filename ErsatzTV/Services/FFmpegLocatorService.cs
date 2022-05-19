using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Services;

public class FFmpegLocatorService : IHostedService
{
    private readonly ILogger<FFmpegLocatorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public FFmpegLocatorService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FFmpegLocatorService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IFFmpegLocator ffmpegLocator = scope.ServiceProvider.GetRequiredService<IFFmpegLocator>();

        // check for ffmpeg and ffprobe in the last known/configured location
        // otherwise search using which/where and save any located executables
        Option<string> maybeFFmpegPath = await ffmpegLocator.ValidatePath("ffmpeg", ConfigElementKey.FFmpegPath);
        maybeFFmpegPath.Match(
            path => _logger.LogInformation("Located ffmpeg at {Path}", path),
            () => _logger.LogWarning("Failed to locate ffmpeg executable"));

        Option<string> maybeFFprobePath =
            await ffmpegLocator.ValidatePath("ffprobe", ConfigElementKey.FFprobePath);
        maybeFFprobePath.Match(
            path => _logger.LogInformation("Located ffprobe at {Path}", path),
            () => _logger.LogWarning("Failed to locate ffprobe executable"));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
