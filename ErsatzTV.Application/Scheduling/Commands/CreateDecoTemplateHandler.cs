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
        Validation<BaseError, DecoTemplate> validation = await Validate(request);
        return await validation.Apply(profile => PersistDecoTemplate(dbContext, profile));
    }

    private static async Task<DecoTemplateViewModel> PersistDecoTemplate(TvContext dbContext, DecoTemplate decoTemplate)
    {
        await dbContext.DecoTemplates.AddAsync(decoTemplate);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(decoTemplate);
    }

    private static Task<Validation<BaseError, DecoTemplate>> Validate(CreateDecoTemplate request) =>
        Task.FromResult(
            ValidateName(request).Map(
                name => new DecoTemplate
                {
                    DecoTemplateGroupId = request.DecoTemplateGroupId,
                    Name = name
                }));

    private static Validation<BaseError, string> ValidateName(CreateDecoTemplate createDecoTemplate) =>
        createDecoTemplate.NotEmpty(x => x.Name)
            .Bind(_ => createDecoTemplate.NotLongerThan(50)(x => x.Name));
}
