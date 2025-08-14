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
        Validation<BaseError, DecoGroup> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistDecoGroup(dbContext, profile));
    }

    private static async Task<DecoGroupViewModel> PersistDecoGroup(TvContext dbContext, DecoGroup decoGroup)
    {
        await dbContext.DecoGroups.AddAsync(decoGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(decoGroup);
    }

    private static Task<Validation<BaseError, DecoGroup>> Validate(TvContext dbContext, CreateDecoGroup request) =>
        ValidateName(dbContext, request).MapT(name => new DecoGroup { Name = name, Decos = [] });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateDecoGroup createDecoGroup)
    {
        Validation<BaseError, string> result1 = createDecoGroup.NotEmpty(c => c.Name)
            .Bind(_ => createDecoGroup.NotLongerThan(50)(c => c.Name));

        int duplicateNameCount = await dbContext.DecoGroups
            .CountAsync(ps => ps.Name == createDecoGroup.Name);

        var result2 = Optional(duplicateNameCount)
            .Where(count => count == 0)
            .ToValidation<BaseError>("Deco group name must be unique");

        return (result1, result2).Apply((_, _) => createDecoGroup.Name);
    }
}