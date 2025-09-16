using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class GetProgramScheduleItemsHandler(IDbContextFactory<TvContext> dbContextFactory) :
    IRequestHandler<GetProgramScheduleItems, List<ProgramScheduleItemViewModel>>
{
    public async Task<List<ProgramScheduleItemViewModel>> Handle(
        GetProgramScheduleItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<ProgramSchedule> maybeProgramSchedule =
            await dbContext.ProgramSchedules.SelectOneAsync(ps => ps.Id, ps => ps.Id == request.Id, cancellationToken);

        return await dbContext.ProgramScheduleItems
            .Filter(psi => psi.ProgramScheduleId == request.Id)
            .Include(i => i.Collection)
            .Include(i => i.MultiCollection)
            .Include(i => i.SmartCollection)
            .Include(i => i.RerunCollection)
            .Include(i => i.Playlist)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .ThenInclude(am => am.Artwork)
            .Include(i => i.PreRollFiller)
            .Include(i => i.MidRollFiller)
            .Include(i => i.PostRollFiller)
            .Include(i => i.TailFiller)
            .Include(i => i.FallbackFiller)
            .Include(i => i.ProgramScheduleItemWatermarks)
            .ThenInclude(i => i.Watermark)
            .Include(i => i.ProgramScheduleItemGraphicsElements)
            .ThenInclude(i => i.GraphicsElement)
            .ToListAsync(cancellationToken)
            .Map(programScheduleItems => programScheduleItems.Map(ProjectToViewModel)
                .Map(psi => EnforceProperties(maybeProgramSchedule, psi)).ToList());
    }

    // shuffled schedule items supports a limited set of property values
    private static ProgramScheduleItemViewModel EnforceProperties(
        Option<ProgramSchedule> maybeProgramSchedule,
        ProgramScheduleItemViewModel item)
    {
        foreach (ProgramSchedule programSchedule in maybeProgramSchedule)
        {
            if (programSchedule.ShuffleScheduleItems)
            {
                item = item with { StartType = StartType.Dynamic };
                if (item.PlayoutMode == PlayoutMode.Flood)
                {
                    item = item with { PlayoutMode = PlayoutMode.One };
                }
            }

            if (item.PlaybackOrder is PlaybackOrder.ShuffleInOrder)
            {
                item = item with { FillWithGroupMode = FillWithGroupMode.None };
            }

            if (item.CollectionType is CollectionType.Playlist or CollectionType.RerunFirstRun
                or CollectionType.RerunRerun)
            {
                item = item with { PlaybackOrder = PlaybackOrder.None };
            }
        }

        return item;
    }
}
