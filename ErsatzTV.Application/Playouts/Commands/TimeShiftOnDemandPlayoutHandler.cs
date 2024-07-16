using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class TimeShiftOnDemandPlayoutHandler(
    IPlayoutTimeShifter playoutTimeShifter,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<TimeShiftOnDemandPlayout, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(TimeShiftOnDemandPlayout request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<Playout> maybePlayout = await dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.Items)
                .Include(p => p.Anchor)
                .Include(p => p.ProgramScheduleAnchors)
                .SelectOneAsync(p => p.Channel.Number, p => p.Channel.Number == request.ChannelNumber);

            foreach (Playout playout in maybePlayout)
            {
                playoutTimeShifter.TimeShift(playout, request.Now, request.Force);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}
