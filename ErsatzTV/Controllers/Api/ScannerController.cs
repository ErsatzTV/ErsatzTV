using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/scan/{scanId:guid}")]
public class ScannerController(IScannerProxyService scannerProxyService)
{
    [HttpPost("progress")]
    [EndpointSummary("Scanner Progress update")]
    public async Task<IActionResult> Progress(Guid scanId, [FromBody] decimal percentComplete)
    {
        await scannerProxyService.Progress(scanId, percentComplete);
        return new OkResult();
    }
}
