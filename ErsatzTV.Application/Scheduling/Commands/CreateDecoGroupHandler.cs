using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateDecoGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateDecoGroup, Either<BaseError, DecoGroupViewModel>>
{
    public async Task<Either<BaseError, DecoGroupViewModel>> Handle(
        CreateDecoGroup request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, DecoGroup> validation = await Validate(request);
        return await validation.Apply(profile => PersistDecoGroup(dbContext, profile));
    }

    private static async Task<DecoGroupViewModel> PersistDecoGroup(TvContext dbContext, DecoGroup decoGroup)
    {
        await dbContext.DecoGroups.AddAsync(decoGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(decoGroup);
    }

    private static Task<Validation<BaseError, DecoGroup>> Validate(CreateDecoGroup request) =>
        Task.FromResult(ValidateName(request).Map(name => new DecoGroup { Name = name, Decos = [] }));

    private static Validation<BaseError, string> ValidateName(CreateDecoGroup createDecoGroup) =>
        createDecoGroup.NotEmpty(x => x.Name)
            .Bind(_ => createDecoGroup.NotLongerThan(50)(x => x.Name));
}
