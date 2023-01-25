using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class GetProgramScheduleItemsHandler :
    IRequestHandler<GetProgramScheduleItems, List<ProgramScheduleItemViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetProgramScheduleItemsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<ProgramScheduleItemViewModel>> Handle(
        GetProgramScheduleItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<ProgramSchedule> maybeProgramSchedule =
            await dbContext.ProgramSchedules.SelectOneAsync(ps => ps.Id, ps => ps.Id == request.Id);

        return await dbContext.ProgramScheduleItems
            .Filter(psi => psi.ProgramScheduleId == request.Id)
            .Include(i => i.Collection)
            .Include(i => i.MultiCollection)
            .Include(i => i.SmartCollection)
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
            .Include(i => i.MidRollEnterFiller)
            .Include(i => i.MidRollFiller)
            .Include(i => i.MidRollExitFiller)
            .Include(i => i.PostRollFiller)
            .Include(i => i.TailFiller)
            .Include(i => i.FallbackFiller)
            .Include(i => i.Watermark)
            .ToListAsync(cancellationToken)
            .Map(
                programScheduleItems => programScheduleItems.Map(ProjectToViewModel)
                    .Map(psi => EnforceProperties(maybeProgramSchedule, psi)).ToList());
    }

    // shuffled schedule items supports a limited set of properly values
    private ProgramScheduleItemViewModel EnforceProperties(
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
        }

        return item;
    }
}
