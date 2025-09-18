using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoById, Option<DecoViewModel>>
{
    public async Task<Option<DecoViewModel>> Handle(GetDecoById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Decos
            .AsNoTracking()
            .Include(d => d.DecoGroup)
            .Include(d => d.DecoWatermarks)
            .ThenInclude(d => d.Watermark)
            .Include(d => d.DecoGraphicsElements)
            .ThenInclude(d => d.GraphicsElement)

            .Include(d => d.DefaultFillerCollection)
            .Include(d => d.DefaultFillerMultiCollection)
            .Include(d => d.DefaultFillerSmartCollection)
            .Include(d => d.DefaultFillerMediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DefaultFillerMediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DefaultFillerMediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DefaultFillerMediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .ThenInclude(am => am.Artwork)

            .Include(d => d.DeadAirFallbackCollection)
            .Include(d => d.DeadAirFallbackMultiCollection)
            .Include(d => d.DeadAirFallbackSmartCollection)
            .Include(d => d.DeadAirFallbackMediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DeadAirFallbackMediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DeadAirFallbackMediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.DeadAirFallbackMediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .ThenInclude(am => am.Artwork)

            .SelectOneAsync(b => b.Id, b => b.Id == request.DecoId, cancellationToken)
            .MapT(Mapper.ProjectToViewModel);
    }
}
