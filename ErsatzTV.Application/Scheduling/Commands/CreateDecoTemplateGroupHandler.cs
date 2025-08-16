using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateDecoTemplateGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateDecoTemplateGroup, Either<BaseError, DecoTemplateGroupViewModel>>
{
    public async Task<Either<BaseError, DecoTemplateGroupViewModel>> Handle(
        CreateDecoTemplateGroup request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, DecoTemplateGroup> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistDecoTemplateGroup(dbContext, profile));
    }

    private static async Task<DecoTemplateGroupViewModel> PersistDecoTemplateGroup(
        TvContext dbContext,
        DecoTemplateGroup decoDecoTemplateGroup)
    {
        await dbContext.DecoTemplateGroups.AddAsync(decoDecoTemplateGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(decoDecoTemplateGroup);
    }

    private static Task<Validation<BaseError, DecoTemplateGroup>> Validate(
        TvContext dbContext,
        CreateDecoTemplateGroup request) =>
        ValidateName(dbContext, request).MapT(name => new DecoTemplateGroup { Name = name, DecoTemplates = [] });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateDecoTemplateGroup createDecoTemplateGroup)
    {
        Validation<BaseError, string> result1 = createDecoTemplateGroup.NotEmpty(c => c.Name)
            .Bind(_ => createDecoTemplateGroup.NotLongerThan(50)(c => c.Name));

        int duplicateNameCount = await dbContext.DecoTemplateGroups
            .CountAsync(ps => ps.Name == createDecoTemplateGroup.Name);

        var result2 = Optional(duplicateNameCount)
            .Where(count => count == 0)
            .ToValidation<BaseError>("Deco template group name must be unique");

        return (result1, result2).Apply((_, _) => createDecoTemplateGroup.Name);
    }
}
