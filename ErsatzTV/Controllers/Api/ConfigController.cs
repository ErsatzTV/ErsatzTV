using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Configuration;
using ErsatzTV.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class ConfigController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/config/playout", Name = "GetPlayoutSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Get playout settings")]
    [EndpointGroupName("general")]
    public async Task<PlayoutSettingsViewModel> GetPlayoutSettings(CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlayoutSettings(), cancellationToken);

    [HttpPut("/api/config/playout", Name = "UpdatePlayoutSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Update playout settings")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdatePlayoutSettings(
        [Required] [FromBody] PlayoutSettingsViewModel settings,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new UpdatePlayoutSettings(settings), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpGet("/api/config/xmltv", Name = "GetXmltvSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Get XMLTV settings")]
    [EndpointGroupName("general")]
    public async Task<XmltvSettingsViewModel> GetXmltvSettings(CancellationToken cancellationToken) =>
        await mediator.Send(new GetXmltvSettings(), cancellationToken);

    [HttpPut("/api/config/xmltv", Name = "UpdateXmltvSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Update XMLTV settings")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateXmltvSettings(
        [Required] [FromBody] XmltvSettingsViewModel settings,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new UpdateXmltvSettings(settings), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpGet("/api/config/logging", Name = "GetLoggingSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Get logging settings")]
    [EndpointGroupName("general")]
    public async Task<LoggingSettingsViewModel> GetLoggingSettings(CancellationToken cancellationToken) =>
        await mediator.Send(new GetLoggingSettings(), cancellationToken);

    [HttpPut("/api/config/logging", Name = "UpdateLoggingSettings")]
    [Tags("Configuration")]
    [EndpointSummary("Update logging settings")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateLoggingSettings(
        [Required] [FromBody] LoggingSettingsViewModel settings,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new UpdateLoggingSettings(settings), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpGet("/api/config/library-refresh-interval", Name = "GetLibraryRefreshInterval")]
    [Tags("Configuration")]
    [EndpointSummary("Get library refresh interval")]
    [EndpointGroupName("general")]
    public async Task<int> GetLibraryRefreshInterval(CancellationToken cancellationToken) =>
        await mediator.Send(new GetLibraryRefreshInterval(), cancellationToken);

    [HttpPut("/api/config/library-refresh-interval", Name = "UpdateLibraryRefreshInterval")]
    [Tags("Configuration")]
    [EndpointSummary("Update library refresh interval")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateLibraryRefreshInterval(
        [Required] [FromBody] UpdateLibraryRefreshIntervalRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateLibraryRefreshInterval(request.Interval), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}

// Request models
public record UpdateLibraryRefreshIntervalRequest(int Interval);
