using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ReplaceTemplateItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ReplaceTemplateItems, Either<BaseError, List<TemplateItemViewModel>>>
{
    public async Task<Either<BaseError, List<TemplateItemViewModel>>> Handle(
        ReplaceTemplateItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Template> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => Persist(dbContext, request, ps));
    }

    private static async Task<List<TemplateItemViewModel>> Persist(
        TvContext dbContext,
        ReplaceTemplateItems request,
        Template template)
    {
        template.Name = request.Name;
        template.DateUpdated = DateTime.UtcNow;

        dbContext.RemoveRange(template.Items);
        template.Items = request.Items.Map(i => BuildItem(template, i)).ToList();

        await dbContext.SaveChangesAsync();

        // TODO: refresh any playouts that use this schedule
        // foreach (Playout playout in programSchedule.Playouts)
        // {
        //     await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh));
        // }

        await dbContext.Entry(template)
            .Collection(t => t.Items)
            .Query()
            .Include(i => i.Block)
            .LoadAsync();

        return template.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static TemplateItem BuildItem(Template template, ReplaceTemplateItem item) =>
        new()
        {
            TemplateId = template.Id,
            BlockId = item.BlockId,
            StartTime = item.StartTime
        };

    private static Task<Validation<BaseError, Template>> Validate(TvContext dbContext, ReplaceTemplateItems request) =>
        TemplateMustExist(dbContext, request.TemplateId)
            .BindT(template => TemplateItemsMustBeValid(dbContext, template, request));

    private static async Task<Validation<BaseError, Template>> TemplateItemsMustBeValid(
        TvContext dbContext,
        Template template,
        ReplaceTemplateItems request)
    {
        var allBlockIds = request.Items.Map(i => i.BlockId).Distinct().ToList();

        Dictionary<int, Block> allBlocks = await dbContext.Blocks
            .AsNoTracking()
            .Filter(b => allBlockIds.Contains(b.Id))
            .ToListAsync()
            .Map(list => list.ToDictionary(b => b.Id, b => b));

        var allTemplateItems = request.Items.Map(
                i =>
                {
                    Block block = allBlocks[i.BlockId];
                    return new BlockTemplateItem(
                        i.BlockId,
                        i.StartTime,
                        i.StartTime + TimeSpan.FromMinutes(block.Minutes));
                })
            .ToList();

        foreach (BlockTemplateItem item in allTemplateItems)
        {
            foreach (BlockTemplateItem otherItem in allTemplateItems)
            {
                if (item == otherItem)
                {
                    continue;
                }

                if (item.StartTime < otherItem.EndTime && otherItem.StartTime < item.EndTime)
                {
                    return BaseError.New(
                        $"Block from {item.StartTime} to {item.EndTime} intersects block from {otherItem.StartTime} to {otherItem.EndTime}");
                }
            }
        }

        return template;
    }

    private static Task<Validation<BaseError, Template>> TemplateMustExist(TvContext dbContext, int templateId) =>
        dbContext.Templates
            .Include(b => b.Items)
            .SelectOneAsync(b => b.Id, b => b.Id == templateId)
            .Map(o => o.ToValidation<BaseError>("[TemplateId] does not exist."));

    private sealed record BlockTemplateItem(int BlockId, TimeSpan StartTime, TimeSpan EndTime);
}
