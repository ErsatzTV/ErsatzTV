using System.Net;
using System.Text.RegularExpressions;
using ErsatzTV.Core;

namespace ErsatzTV.Services.RunOnce;

public class EndpointValidatorService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EndpointValidatorService> _logger;

    public EndpointValidatorService(IConfiguration configuration, ILogger<EndpointValidatorService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string urls = _configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
        if (urls.Split(";").Length > 1)
        {
            throw new NotSupportedException($"Multiple endpoints are not supported: {urls}");
        }

        const string PATTERN = @"http:\/\/(.*):(\d+)";
        Match match = Regex.Match(urls, PATTERN);
        if (match.Success)
        {
            string hostname = match.Groups[1].Value;
            Settings.ListenPort = int.Parse(match.Groups[2].Value);

            // IP address must be 0.0.0.0 or 127.0.0.1
            if (IPAddress.TryParse(hostname, out IPAddress address))
            {
                if (!address.Equals(IPAddress.Parse("0.0.0.0")) && !IPAddress.IsLoopback(address))
                {
                    throw new NotSupportedException($"Endpoint MUST include loopback: {urls}");
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Invalid endpoint format: {urls}");
        }

        string baseUrl = Environment.GetEnvironmentVariable("ETV_BASE_URL");

        _logger.LogInformation(
            "Server will listen on port {Port} - try UI at {UI}",
            Settings.ListenPort,
            $"http://localhost:{Settings.ListenPort}{baseUrl}");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
