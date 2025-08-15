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

        // add new text elements
        var newTextPaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsTextTemplatesFolder)
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (var path in newTextPaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Text
            };

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        // add new image elements
        var newImagePaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsImageTemplatesFolder)
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (var path in newImagePaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Image
            };

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        // add new subtitle elements
        var newSubtitlePaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsSubtitleTemplatesFolder)
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (var path in newSubtitlePaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Subtitle
            };

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}