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

        dbContext.RemoveRange(template.Items);
        template.Items = request.Items.Map(i => BuildItem(template, i)).ToList();

        await dbContext.SaveChangesAsync();

        // TODO: refresh any playouts that use this schedule
        // foreach (Playout playout in programSchedule.Playouts)
        // {
        //     await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh));
        // }

        return template.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static TemplateItem BuildItem(Template template, ReplaceTemplateItem item) =>
        new()
        {
            TemplateId = template.Id,
            BlockId = item.BlockId,
        };

    private static Task<Validation<BaseError, Template>> Validate(TvContext dbContext, ReplaceTemplateItems request) =>
        TemplateMustExist(dbContext, request.TemplateId);

    private static Task<Validation<BaseError, Template>> TemplateMustExist(TvContext dbContext, int templateId) =>
        dbContext.Templates
            .Include(b => b.Items)
            .SelectOneAsync(b => b.Id, b => b.Id == templateId)
            .Map(o => o.ToValidation<BaseError>("[TemplateId] does not exist."));
}
