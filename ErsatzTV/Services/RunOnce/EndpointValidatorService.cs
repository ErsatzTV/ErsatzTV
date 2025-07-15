using ErsatzTV.Core;

namespace ErsatzTV.Services.RunOnce;

public class EndpointValidatorService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EndpointValidatorService> _logger;

    public EndpointValidatorService(IConfiguration configuration, ILogger<EndpointValidatorService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _logger.LogInformation(
            "Server will listen on streaming port {StreamingPort}, UI port {UiPort} - try UI at {UI}",
            Settings.StreamingPort,
            Settings.UiPort,
            $"http://localhost:{Settings.UiPort}{SystemEnvironment.BaseUrl}");
    }
}
