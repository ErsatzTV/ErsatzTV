using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateDecoTemplateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateDecoTemplate, Either<BaseError, DecoTemplateViewModel>>
{
    public async Task<Either<BaseError, DecoTemplateViewModel>> Handle(
        CreateDecoTemplate request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, DecoTemplate> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistDecoTemplate(dbContext, profile));
    }

    private static async Task<DecoTemplateViewModel> PersistDecoTemplate(TvContext dbContext, DecoTemplate decoTemplate)
    {
        await dbContext.DecoTemplates.AddAsync(decoTemplate);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(decoTemplate).Reference(dt => dt.DecoTemplateGroup).LoadAsync();
        return Mapper.ProjectToViewModel(decoTemplate);
    }

    private static async Task<Validation<BaseError, DecoTemplate>> Validate(
        TvContext dbContext,
        CreateDecoTemplate request) =>
        await ValidateDecoTemplateName(dbContext, request).MapT(name => new DecoTemplate
        {
            DecoTemplateGroupId = request.DecoTemplateGroupId,
            Name = name
        });

    private static async Task<Validation<BaseError, string>> ValidateDecoTemplateName(
        TvContext dbContext,
        CreateDecoTemplate request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Deco template name \"{request.Name}\" is invalid");
        }

        bool duplicateName = await dbContext.DecoTemplates
            .AnyAsync(r => r.DecoTemplateGroupId == request.DecoTemplateGroupId && r.Name == request.Name);

        return duplicateName
            ? BaseError.New($"A deco template named \"{request.Name}\" already exists in that deco template group")
            : Success<BaseError, string>(request.Name);
    }
}
