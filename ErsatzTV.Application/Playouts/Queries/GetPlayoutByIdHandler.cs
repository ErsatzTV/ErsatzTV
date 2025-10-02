using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlayoutById, Option<PlayoutNameViewModel>>
{
    public async Task<Option<PlayoutNameViewModel>> Handle(
        GetPlayoutById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.ProgramSchedule)
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId, cancellationToken)
            .MapT(p => new PlayoutNameViewModel(
                p.Id,
                p.ScheduleKind,
                p.Channel.Name,
                p.Channel.Number,
                p.Channel.PlayoutMode,
                p.ProgramScheduleId == null ? string.Empty : p.ProgramSchedule.Name,
                p.ScheduleFile,
                p.DailyRebuildTime,
                p.BuildStatus));
    }
}
