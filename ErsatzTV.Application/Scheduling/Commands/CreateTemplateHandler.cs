using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateTemplateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateTemplate, Either<BaseError, TemplateViewModel>>
{
    public async Task<Either<BaseError, TemplateViewModel>> Handle(
        CreateTemplate request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Template> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistTemplate(dbContext, profile));
    }

    private static async Task<TemplateViewModel> PersistTemplate(TvContext dbContext, Template template)
    {
        await dbContext.Templates.AddAsync(template);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(template).Reference(t => t.TemplateGroup).LoadAsync();
        return Mapper.ProjectToViewModel(template);
    }

    private static async Task<Validation<BaseError, Template>> Validate(TvContext dbContext, CreateTemplate request) =>
        await ValidateTemplateName(dbContext, request).MapT(name => new Template
        {
            TemplateGroupId = request.TemplateGroupId,
            Name = name
        });

    private static async Task<Validation<BaseError, string>> ValidateTemplateName(
        TvContext dbContext,
        CreateTemplate request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Template name \"{request.Name}\" is invalid");
        }

        Option<Template> maybeExisting = await dbContext.Templates
            .FirstOrDefaultAsync(r => r.TemplateGroupId == request.TemplateGroupId && r.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A template named \"{request.Name}\" already exists in that template group")
            : Success<BaseError, string>(request.Name);
    }
}
