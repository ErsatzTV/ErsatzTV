using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Search;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/scan/{scanId:guid}")]
public class ScannerController(
    IScannerProxyService scannerProxyService,
    ChannelWriter<ISearchIndexBackgroundServiceRequest> channelWriter)
{
    [HttpPost("progress")]
    [EndpointSummary("Scanner progress update")]
    public async Task<IActionResult> Progress(Guid scanId, [FromBody] decimal percentComplete)
    {
        await scannerProxyService.Progress(scanId, percentComplete);
        return new OkResult();
    }

    [HttpPost("items/reindex")]
    [EndpointSummary("Scanner reindex items in search index")]
    public async Task<IActionResult> UpdateItems(
        Guid scanId,
        [FromBody] List<int> itemsToUpdate,
        CancellationToken cancellationToken)
    {
        if (scannerProxyService.IsActive(scanId))
        {
            await channelWriter.WriteAsync(new ReindexMediaItems(itemsToUpdate), cancellationToken);
        }

        return new OkResult();
    }

    [HttpPost("items/remove")]
    [EndpointSummary("Scanner remove items from search index")]
    public async Task<IActionResult> RemoveItems(
        Guid scanId,
        [FromBody] List<int> itemsToRemove,
        CancellationToken cancellationToken)
    {
        if (scannerProxyService.IsActive(scanId))
        {
            await channelWriter.WriteAsync(new RemoveMediaItems(itemsToRemove), cancellationToken);
        }

        return new OkResult();
    }
}
