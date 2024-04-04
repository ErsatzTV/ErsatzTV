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
        Validation<BaseError, DecoTemplateGroup> validation = await Validate(request);
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

    private static Task<Validation<BaseError, DecoTemplateGroup>> Validate(CreateDecoTemplateGroup request) =>
        Task.FromResult(ValidateName(request).Map(name => new DecoTemplateGroup { Name = name, DecoTemplates = [] }));

    private static Validation<BaseError, string> ValidateName(CreateDecoTemplateGroup createDecoTemplateGroup) =>
        createDecoTemplateGroup.NotEmpty(x => x.Name)
            .Bind(_ => createDecoTemplateGroup.NotLongerThan(50)(x => x.Name));
}
