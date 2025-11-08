using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Graphics;

public class RefreshGraphicsElementsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ILocalFileSystem localFileSystem,
    IGraphicsElementLoader graphicsElementLoader,
    ILogger<RefreshGraphicsElementsHandler> logger)
    : IRequestHandler<RefreshGraphicsElements>
{
    public async Task Handle(RefreshGraphicsElements request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // cleanup existing elements
        List<GraphicsElement> allExisting = await dbContext.GraphicsElements
            .ToListAsync(cancellationToken);

        var missing = allExisting
            .Where(e => !localFileSystem.FileExists(e.Path) || Path.GetExtension(e.Path) != ".yml")
            .ToList();

        foreach (GraphicsElement existing in missing)
        {
            logger.LogWarning(
                "Removing graphics element that references non-existing file {File}",
                existing.Path);

            dbContext.GraphicsElements.Remove(existing);
        }

        foreach (GraphicsElement existing in allExisting.Except(missing))
        {
            await TryRefreshName(existing, cancellationToken);
        }

        // add new text elements
        var newTextPaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsTextTemplatesFolder, "*.yml")
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (string path in newTextPaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Text
            };

            await TryRefreshName(graphicsElement, cancellationToken);

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        // add new image elements
        var newImagePaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsImageTemplatesFolder, "*.yml")
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (string path in newImagePaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Image
            };

            await TryRefreshName(graphicsElement, cancellationToken);

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        // add new motion elements
        var newMotionPaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsMotionTemplatesFolder, "*.yml")
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (string path in newMotionPaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Motion
            };

            await TryRefreshName(graphicsElement, cancellationToken);

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        // add new subtitle elements
        var newSubtitlePaths = localFileSystem.ListFiles(FileSystemLayout.GraphicsElementsSubtitleTemplatesFolder, "*.yml")
            .Where(f => allExisting.All(e => e.Path != f))
            .ToList();

        foreach (string path in newSubtitlePaths)
        {
            logger.LogDebug("Adding new graphics element from file {File}", path);

            var graphicsElement = new GraphicsElement
            {
                Path = path,
                Kind = GraphicsElementKind.Subtitle
            };

            await TryRefreshName(graphicsElement, cancellationToken);

            await dbContext.AddAsync(graphicsElement, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TryRefreshName(GraphicsElement graphicsElement, CancellationToken cancellationToken)
    {
        graphicsElement.Name = null;
        Option<string> maybeName = await graphicsElementLoader.TryLoadName(graphicsElement.Path, cancellationToken);
        foreach (string name in maybeName)
        {
            graphicsElement.Name = name;
        }
    }
}
