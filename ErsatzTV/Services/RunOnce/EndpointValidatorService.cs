using ErsatzTV.Core;

namespace ErsatzTV.Services.RunOnce;

public class EndpointValidatorService(ILogger<EndpointValidatorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        logger.LogInformation(
            "Server will listen on streaming port {StreamingPort}, UI port {UiPort} - try UI at {UI}",
            Settings.StreamingPort,
            Settings.UiPort,
            $"http://localhost:{Settings.UiPort}{SystemEnvironment.BaseUrl}");
    }
}
