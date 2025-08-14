using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateTemplateGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateTemplateGroup, Either<BaseError, TemplateGroupViewModel>>
{
    public async Task<Either<BaseError, TemplateGroupViewModel>> Handle(
        CreateTemplateGroup request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, TemplateGroup> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistTemplateGroup(dbContext, profile));
    }

    private static async Task<TemplateGroupViewModel> PersistTemplateGroup(
        TvContext dbContext,
        TemplateGroup templateGroup)
    {
        await dbContext.TemplateGroups.AddAsync(templateGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(templateGroup);
    }

    private static Task<Validation<BaseError, TemplateGroup>> Validate(TvContext dbContext, CreateTemplateGroup request) =>
        ValidateName(dbContext, request).MapT(name => new TemplateGroup { Name = name, Templates = [] });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateTemplateGroup createTemplateGroup)
    {
        Validation<BaseError, string> result1 = createTemplateGroup.NotEmpty(c => c.Name)
            .Bind(_ => createTemplateGroup.NotLongerThan(50)(c => c.Name));

        int duplicateNameCount = await dbContext.TemplateGroups
            .CountAsync(ps => ps.Name == createTemplateGroup.Name);

        var result2 = Optional(duplicateNameCount)
            .Where(count => count == 0)
            .ToValidation<BaseError>("Template group name must be unique");

        return (result1, result2).Apply((_, _) => createTemplateGroup.Name);
    }
}
