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
        if (classicContext is not null && classicContext.ScheduleId > 0)
        {
            return await GetClassicDetails(classicContext, cancellationToken);
        }

        var blockContext = JsonConvert.DeserializeObject<BlockSchedulingContext>(request.SerializedContext);
        if (blockContext is not null && blockContext.BlockId > 0)
        {
            return await GetBlockDetails(blockContext, cancellationToken);
        }

        return request.SerializedContext;
    }

    private async Task<Option<string>> GetClassicDetails(
        ClassicSchedulingContext classicContext,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        string scheduleName = await dbContext.ProgramSchedules
            .Where(s => s.Id == classicContext.ScheduleId)
            .Select(s => s.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var scheduleItem = await dbContext.ProgramScheduleItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(si => si.Collection)
            .Include(si => si.MultiCollection)
            .Include(si => si.Playlist)
            .Include(si => si.RerunCollection)
            .Include(si => si.SmartCollection)
            .SingleOrDefaultAsync(s => s.Id == classicContext.ItemId, cancellationToken);

        ClassicContextScheduleItem item;
        if (scheduleItem is not null)
        {
            string collectionName = scheduleItem.CollectionType switch
            {
                CollectionType.Collection => scheduleItem.Collection.Name,
                CollectionType.MultiCollection => scheduleItem.MultiCollection.Name,
                CollectionType.Playlist => scheduleItem.Playlist.Name,
                CollectionType.RerunRerun or CollectionType.RerunFirstRun => scheduleItem.RerunCollection.Name,
                CollectionType.SmartCollection => scheduleItem.SmartCollection.Name,
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

    private async Task<Option<string>> GetBlockDetails(BlockSchedulingContext blockContext, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var block = await dbContext.Blocks
            .Include(b => b.BlockGroup)
            .SingleOrDefaultAsync(s => s.Id == blockContext.BlockId, cancellationToken);

        var blockItem = await dbContext.BlockItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(si => si.Collection)
            .Include(si => si.MultiCollection)
            .Include(si => si.SmartCollection)
            .SingleOrDefaultAsync(s => s.Id == blockContext.BlockItemId, cancellationToken);

        BlockContextBlockItem item;
        if (blockItem is not null)
        {
            string collectionName = blockItem.CollectionType switch
            {
                CollectionType.Collection => blockItem.Collection.Name,
                CollectionType.MultiCollection => blockItem.MultiCollection.Name,
                CollectionType.SmartCollection => blockItem.SmartCollection.Name,
                _ => null
            };

            item = new BlockContextBlockItem(blockItem.Id, blockItem.CollectionType, collectionName);
        }
        else
        {
            item = new BlockContextBlockItem(blockContext.BlockItemId, null, null);
        }

        var context = new BlockContext(
            new ContextScheduler("Block", null),
            new BlockContextBlock(
                blockContext.BlockId,
                block?.BlockGroup?.Name ?? string.Empty,
                block?.Name ?? string.Empty),
            item,
            new ContextEnumerator(blockContext.Enumerator, blockContext.Seed, blockContext.Index));

        return JsonSerializer.Serialize(context, Options);
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

    private sealed record BlockContext(
        ContextScheduler Scheduler,
        BlockContextBlock Block,
        BlockContextBlockItem BlockItem,
        ContextEnumerator Enumerator);

    private sealed record BlockContextBlock(int Id, string Group, string Name);

    private sealed record BlockContextBlockItem(int Id, CollectionType? CollectionType, string CollectionName);
}
