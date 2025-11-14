using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Scheduling.Mapper;

namespace ErsatzTV.Application.Scheduling;

public class CopyTemplateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CopyTemplate, Either<BaseError, TemplateViewModel>>
{
    public async Task<Either<BaseError, TemplateViewModel>> Handle(
        CopyTemplate request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            Validation<BaseError, Template> validation = await Validate(dbContext, request, cancellationToken);
            return await validation.Apply(p => PerformCopy(dbContext, p, request, cancellationToken));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<TemplateViewModel> PerformCopy(
        TvContext dbContext,
        Template template,
        CopyTemplate request,
        CancellationToken cancellationToken)
    {
        DetachEntity(dbContext, template);
        template.Name = request.NewTemplateName;
        template.TemplateGroup = null;
        template.TemplateGroupId = request.NewTemplateGroupId;

        foreach (TemplateItem item in template.Items)
        {
            DetachEntity(dbContext, item);
            item.TemplateId = 0;
            item.Template = template;
        }

        await dbContext.Templates.AddAsync(template, cancellationToken);
        await dbContext.TemplateItems.AddRangeAsync(template.Items, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(template).Reference(b => b.TemplateGroup).LoadAsync(cancellationToken);

        return ProjectToViewModel(template);
    }

    private static async Task<Validation<BaseError, Template>> Validate(
        TvContext dbContext,
        CopyTemplate request,
        CancellationToken cancellationToken) =>
        (await TemplateMustExist(dbContext, request, cancellationToken), await ValidateName(dbContext, request))
        .Apply((template, _) => template);

    private static Task<Validation<BaseError, Template>> TemplateMustExist(
        TvContext dbContext,
        CopyTemplate request,
        CancellationToken cancellationToken) =>
        dbContext.Templates
            .AsNoTracking()
            .Include(t => t.Items)
            .SelectOneAsync(p => p.Id, p => p.Id == request.TemplateId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Template does not exist."));

    private static async Task<Validation<BaseError, string>> ValidateName(TvContext dbContext, CopyTemplate request)
    {
        List<string> allNames = await dbContext.Templates
            .Where(b => b.TemplateGroupId == request.NewTemplateGroupId)
            .Map(ps => ps.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = request.NotEmpty(c => c.NewTemplateName)
            .Bind(_ => request.NotLongerThan(50)(c => c.NewTemplateName));

        var result2 = Optional(request.NewTemplateName)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("Template name must be unique within the template group.");

        return (result1, result2).Apply((_, _) => request.NewTemplateName);
    }

    private static void DetachEntity<T>(TvContext db, T entity) where T : class
    {
        db.Entry(entity).State = EntityState.Detached;
        if (entity.GetType().GetProperty("Id") is not null)
        {
            entity.GetType().GetProperty("Id")!.SetValue(entity, 0);
        }
    }
}
