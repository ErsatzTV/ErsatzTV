using System.Text.Json;
using System.Text.Json.Serialization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ErsatzTV.Application.ProgramSchedules;

public class ProcessSchedulingContextHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ProcessSchedulingContext, Option<string>>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<Option<string>> Handle(ProcessSchedulingContext request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SerializedContext))
        {
            return Option<string>.None;
        }

        var classicContext = JsonConvert.DeserializeObject<ClassicSchedulingContext>(request.SerializedContext);
        if (classicContext is not null)
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            string scheduleName = await dbContext.ProgramSchedules
                .Where(s => s.Id == classicContext.ScheduleId)
                .Select(s => s.Name)
                .SingleOrDefaultAsync(cancellationToken);

            var scheduleItem = await dbContext.ProgramScheduleItems
                .AsNoTracking()
                .AsSplitQuery()
                .Include(si => si.SmartCollection)
                .Include(si => si.Collection)
                .Include(si => si.MultiCollection)
                .Include(si => si.RerunCollection)
                .Include(si => si.Playlist)
                .SingleOrDefaultAsync(s => s.Id == classicContext.ItemId, cancellationToken);

            ClassicContextScheduleItem item;
            if (scheduleItem is not null)
            {
                string collectionName = scheduleItem.CollectionType switch
                {
                    CollectionType.SmartCollection => scheduleItem.SmartCollection.Name,
                    CollectionType.Collection => scheduleItem.Collection.Name,
                    CollectionType.MultiCollection => scheduleItem.MultiCollection.Name,
                    CollectionType.RerunRerun or CollectionType.RerunFirstRun => scheduleItem.RerunCollection.Name,
                    CollectionType.Playlist => scheduleItem.Playlist.Name,
                    _ => null
                };

                item = new ClassicContextScheduleItem(scheduleItem.Id, scheduleItem.CollectionType, collectionName);
            }
            else
            {
                item = new ClassicContextScheduleItem(classicContext.ItemId, null, null);
            }

            var context = new ClassicContext(
                new ContextScheduler("Classic", classicContext.Scheduler),
                new ClassicContextSchedule(classicContext.ScheduleId, scheduleName ?? string.Empty),
                item,
                new ContextEnumerator(classicContext.Enumerator, classicContext.Seed, classicContext.Index));

            return JsonSerializer.Serialize(context, Options);
        }

        return request.SerializedContext;
    }

    private sealed record ClassicContext(
        ContextScheduler Scheduler,
        ClassicContextSchedule Schedule,
        ClassicContextScheduleItem ScheduleItem,
        ContextEnumerator Enumerator);

    private sealed record ContextScheduler(string Type, string Mode);

    private sealed record ClassicContextSchedule(int Id, string Name);

    private sealed record ClassicContextScheduleItem(int Id, CollectionType? CollectionType, string CollectionName);

    private sealed record ContextEnumerator(string Name, int Seed, int Index);
}
