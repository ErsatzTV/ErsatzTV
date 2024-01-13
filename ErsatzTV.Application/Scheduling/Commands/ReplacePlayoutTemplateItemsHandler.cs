using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Scheduling;

public class ReplacePlayoutTemplateItemsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ILogger<ReplacePlayoutTemplateItemsHandler> logger)
    : IRequestHandler<ReplacePlayoutTemplateItems, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(
        ReplacePlayoutTemplateItems request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<Playout> maybePlayout = await dbContext.Playouts
                .Include(p => p.ProgramSchedule)
                .Include(p => p.Templates)
                .ThenInclude(t => t.Template)
                .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

            foreach (Playout playout in maybePlayout)
            {
                PlayoutTemplate[] existing = playout.Templates.ToArray();

                List<ReplacePlayoutTemplate> incoming = request.Items;

                var toAdd = incoming.Filter(x => existing.All(e => e.Id != x.Id)).ToList();
                var toRemove = existing.Filter(e => incoming.All(m => m.Id != e.Id)).ToList();
                var toUpdate = incoming.Except(toAdd).ToList();

                foreach (PlayoutTemplate remove in toRemove)
                {
                    playout.Templates.Remove(remove);
                }

                foreach (ReplacePlayoutTemplate add in toAdd)
                {
                    playout.Templates.Add(
                        new PlayoutTemplate
                        {
                            PlayoutId = playout.Id,
                            Index = add.Index,
                            TemplateId = add.TemplateId,
                            DaysOfWeek = add.DaysOfWeek,
                            DaysOfMonth = add.DaysOfMonth,
                            MonthsOfYear = add.MonthsOfYear
                        });
                }

                foreach (ReplacePlayoutTemplate update in toUpdate)
                {
                    foreach (PlayoutTemplate ex in existing.Filter(x => x.Id == update.Id))
                    {
                        ex.Index = update.Index;
                        ex.TemplateId = update.TemplateId;
                        ex.DaysOfWeek = update.DaysOfWeek;
                        ex.DaysOfMonth = update.DaysOfMonth;
                        ex.MonthsOfYear = update.MonthsOfYear;
                    }
                }
                
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving playout template items");
            return BaseError.New(ex.Message);
        }
    }
}
