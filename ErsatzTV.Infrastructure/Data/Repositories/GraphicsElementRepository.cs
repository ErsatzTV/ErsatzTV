using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class GraphicsElementRepository(IDbContextFactory<TvContext> dbContextFactory) : IGraphicsElementRepository
{
    public async Task<Option<GraphicsElement>> GetGraphicsElementByPath(
        string path,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!path.StartsWith(FileSystemLayout.GraphicsElementsTemplatesFolder, StringComparison.Ordinal))
        {
            path = Path.Combine(FileSystemLayout.GraphicsElementsTemplatesFolder, path);
        }

        return await dbContext.GraphicsElements
            .AsNoTracking()
            .SelectOneAsync(ge => ge.Path, ge => ge.Path == path, cancellationToken);
    }
}
