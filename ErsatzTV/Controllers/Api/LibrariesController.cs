using ErsatzTV.Application.Libraries;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class LibrariesController(ITelevisionRepository televisionRepository, IMediator mediator)
{
    [HttpPost("/api/libraries/{id:int}/scan")]
    public async Task<IActionResult> ResetPlayout(int id) =>
        await mediator.Send(new QueueLibraryScanByLibraryId(id))
            ? new OkResult()
            : new NotFoundResult();

    [HttpPost("/api/libraries/{id:int}/scan-show")]
    public async Task<IActionResult> ScanShow(int id, [FromBody] ScanShowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShowTitle))
        {
            return new BadRequestObjectResult(new { error = "ShowTitle is required" });
        }

        var trimmedTitle = request.ShowTitle.Trim();
        var maybeShowId = await televisionRepository.GetShowIdByTitle(id, trimmedTitle);
        foreach (var showId in maybeShowId)
        {
            bool result = await mediator.Send(new QueueShowScanByLibraryId(id, showId, trimmedTitle, request.DeepScan));

            return result
                ? new OkResult()
                : new BadRequestObjectResult(new { error = "Unable to queue show scan. Library may not exist, may not support single show scanning, or may already be scanning." });
        }

        return new BadRequestObjectResult(new { error = $"Unable to locate show with title {request.ShowTitle} in library {id}" });
    }
}

public record ScanShowRequest(string ShowTitle, bool DeepScan = false);
