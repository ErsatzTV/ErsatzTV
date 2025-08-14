using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ReplaceDecoTemplateItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ReplaceDecoTemplateItems, Either<BaseError, List<DecoTemplateItemViewModel>>>
{
    public async Task<Either<BaseError, List<DecoTemplateItemViewModel>>> Handle(
        ReplaceDecoTemplateItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, DecoTemplate> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => Persist(dbContext, request, ps));
    }

    private static async Task<List<DecoTemplateItemViewModel>> Persist(
        TvContext dbContext,
        ReplaceDecoTemplateItems request,
        DecoTemplate decoTemplate)
    {
        decoTemplate.Name = request.Name;
        decoTemplate.DateUpdated = DateTime.UtcNow;

        dbContext.RemoveRange(decoTemplate.Items);

        // drop items that are invalid
        decoTemplate.Items = request.Items
            .Map(i => BuildItem(decoTemplate, i))
            .Filter(i => i.StartTime < i.EndTime || i.EndTime == TimeSpan.Zero)
            .ToList();

        await dbContext.SaveChangesAsync();

        // TODO: refresh any playouts that use this schedule
        // foreach (Playout playout in programSchedule.Playouts)
        // {
        //     await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh));
        // }

        await dbContext.Entry(decoTemplate)
            .Collection(t => t.Items)
            .Query()
            .Include(i => i.Deco)
            .LoadAsync();

        return decoTemplate.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static DecoTemplateItem BuildItem(DecoTemplate decoTemplate, ReplaceDecoTemplateItem item) =>
        new()
        {
            DecoTemplateId = decoTemplate.Id,
            DecoId = item.DecoId,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        };

    private static Task<Validation<BaseError, DecoTemplate>> Validate(
        TvContext dbContext,
        ReplaceDecoTemplateItems request) =>
        DecoTemplateMustExist(dbContext, request.DecoTemplateId)
            .BindT(decoTemplate => DecoTemplateNameMustBeValid(dbContext, decoTemplate, request));

    private static Task<Validation<BaseError, DecoTemplate>> DecoTemplateMustExist(
        TvContext dbContext,
        int decoTemplateId) =>
        dbContext.DecoTemplates
            .Include(b => b.Items)
            .SelectOneAsync(b => b.Id, b => b.Id == decoTemplateId)
            .Map(o => o.ToValidation<BaseError>("[DecoTemplateId] does not exist."));

    private static async Task<Validation<BaseError, DecoTemplate>> DecoTemplateNameMustBeValid(
        TvContext dbContext,
        DecoTemplate decoTemplate,
        ReplaceDecoTemplateItems request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Deco template name \"{request.Name}\" is invalid");
        }

        Option<DecoTemplate> maybeExisting = await dbContext.DecoTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id != request.DecoTemplateId && d.DecoTemplateGroupId == request.DecoTemplateGroupId &&
                d.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A deco template named \"{request.Name}\" already exists in that deco template group")
            : Success<BaseError, DecoTemplate>(decoTemplate);
    }
}
