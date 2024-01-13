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
        Validation<BaseError, Template> validation = await Validate(request);
        return await validation.Apply(profile => PersistTemplate(dbContext, profile));
    }

    private static async Task<TemplateViewModel> PersistTemplate(TvContext dbContext, Template template)
    {
        await dbContext.Templates.AddAsync(template);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(template);
    }

    private static Task<Validation<BaseError, Template>> Validate(CreateTemplate request) =>
        Task.FromResult(
            ValidateName(request).Map(
                name => new Template
                {
                    TemplateGroupId = request.TemplateGroupId,
                    Name = name
                }));

    private static Validation<BaseError, string> ValidateName(CreateTemplate createTemplate) =>
        createTemplate.NotEmpty(x => x.Name)
            .Bind(_ => createTemplate.NotLongerThan(50)(x => x.Name));
}
