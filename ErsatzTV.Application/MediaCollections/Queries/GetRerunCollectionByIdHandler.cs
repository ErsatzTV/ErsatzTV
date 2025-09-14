using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetRerunCollectionByIdHandler(IDbContextFactory<TvContext> dbContextFactory) :
    IRequestHandler<GetRerunCollectionById, Option<RerunCollectionViewModel>>
{
    public async Task<Option<RerunCollectionViewModel>> Handle(
        GetRerunCollectionById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.RerunCollections
            .AsNoTracking()
            .Include(c => c.Collection)
            .Include(c => c.MultiCollection)
            .Include(c => c.SmartCollection)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id, cancellationToken)
            .MapT(ProjectToViewModel);
    }
}
