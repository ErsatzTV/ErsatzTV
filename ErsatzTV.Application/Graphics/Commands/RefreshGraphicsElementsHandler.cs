using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Graphics;

public class RefreshGraphicsElementsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ILocalFileSystem localFileSystem,
    ILogger<RefreshGraphicsElementsHandler> logger)
    : IRequestHandler<RefreshGraphicsElements>
{
    public async Task Handle(RefreshGraphicsElements request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // cleanup existing elements
        var allExisting = await dbContext.GraphicsElements
            .ToListAsync(cancellationToken);

        foreach (var existing in allExisting.Where(e => !localFileSystem.FileExists(e.Path)))
        {
            logger.LogWarning(
                "Removing graphics element that references non-existing file {File}",
                existing.Path);

            dbContext.GraphicsElements.Remove(existing);
        }

        // add new elements
        var newPaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsTextTemplatesFolder)
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (var path in newPaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Text
            };

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}