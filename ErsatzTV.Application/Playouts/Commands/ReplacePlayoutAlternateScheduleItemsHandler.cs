using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class ReplacePlayoutAlternateScheduleItemsHandler :
    IRequestHandler<ReplacePlayoutAlternateScheduleItems, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ReplacePlayoutAlternateScheduleItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        ReplacePlayoutAlternateScheduleItems request,
        CancellationToken cancellationToken)
    {
        // TODO: validate that items is not empty

        try
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<Playout> maybePlayout = await dbContext.Playouts
                .Include(p => p.ProgramScheduleAlternates)
                .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

            foreach (Playout playout in maybePlayout)
            {
                // exclude highest index
                int maxIndex = request.Items.Map(x => x.Index).Max();
                ReplacePlayoutAlternateSchedule highest = request.Items.First(x => x.Index == maxIndex);

                List<ProgramScheduleAlternate> existing = playout.ProgramScheduleAlternates;

                var incoming = request.Items.Except(new[] { highest }).ToList();

                var toAdd = incoming.Filter(x => existing.All(e => e.Id != x.Id)).ToList();
                var toRemove = existing.Filter(e => incoming.All(m => m.Id != e.Id)).ToList();
                var toUpdate = incoming.Except(toAdd).ToList();

                playout.ProgramScheduleAlternates.RemoveAll(toRemove.Contains);

                foreach (ReplacePlayoutAlternateSchedule add in toAdd)
                {
                    playout.ProgramScheduleAlternates.Add(
                        new ProgramScheduleAlternate
                        {
                            PlayoutId = playout.Id,
                            Index = add.Index,
                            ProgramScheduleId = add.ProgramScheduleId,
                            DaysOfWeek = add.DaysOfWeek,
                            DaysOfMonth = add.DaysOfMonth,
                            MonthsOfYear = add.MonthsOfYear
                        });
                }

                foreach (ReplacePlayoutAlternateSchedule update in toUpdate)
                {
                    foreach (ProgramScheduleAlternate ex in existing.Filter(x => x.Id == update.Id))
                    {
                        ex.Index = update.Index;
                        ex.ProgramScheduleId = update.ProgramScheduleId;
                        ex.DaysOfWeek = update.DaysOfWeek;
                        ex.DaysOfMonth = update.DaysOfMonth;
                        ex.MonthsOfYear = update.MonthsOfYear;
                    }
                }

                // save highest index directly to playout
                if (playout.ProgramScheduleId != highest.ProgramScheduleId)
                {
                    playout.ProgramScheduleId = highest.ProgramScheduleId;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            
            // TODO: should playout be rebuilt? need logic to only do that when needed
            // maybe get min playout item, get max playout item, find range of days
            // compare schedules in range (existing) vs schedules in range (incoming)
            
            return Unit.Default;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}
