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
        Validation<BaseError, TemplateGroup> validation = await Validate(request);
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

    private static Task<Validation<BaseError, TemplateGroup>> Validate(CreateTemplateGroup request) =>
        Task.FromResult(ValidateName(request).Map(name => new TemplateGroup { Name = name, Templates = [] }));

    private static Validation<BaseError, string> ValidateName(CreateTemplateGroup createTemplateGroup) =>
        createTemplateGroup.NotEmpty(x => x.Name)
            .Bind(_ => createTemplateGroup.NotLongerThan(50)(x => x.Name));
}
