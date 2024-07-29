using ErsatzTV.Application.Libraries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class LibrariesController(IMediator mediator)
{
    [HttpPost("/api/libraries/{id:int}/scan")]
    public async Task<IActionResult> ResetPlayout(int id)
    {
        return await mediator.Send(new QueueLibraryScanByLibraryId(id))
            ? new OkResult()
            : new NotFoundResult();
    }
}
