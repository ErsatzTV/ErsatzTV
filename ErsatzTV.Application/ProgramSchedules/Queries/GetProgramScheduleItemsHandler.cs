using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
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
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ProgramScheduleItems
                .Filter(psi => psi.ProgramScheduleId == request.Id)
                .Include(i => i.Collection)
                .Include(i => i.MultiCollection)
                .Include(i => i.SmartCollection)
                .Include(i => i.MediaItem)
                .Include(i => (i as ProgramScheduleItemDuration).TailCollection)
                .Include(i => (i as ProgramScheduleItemDuration).TailMultiCollection)
                .Include(i => (i as ProgramScheduleItemDuration).TailSmartCollection)
                .Include(i => (i as ProgramScheduleItemDuration).TailMediaItem)
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
                .ToListAsync(cancellationToken)
                .Map(programScheduleItems => programScheduleItems.Map(ProjectToViewModel).ToList());
        }
    }
}
