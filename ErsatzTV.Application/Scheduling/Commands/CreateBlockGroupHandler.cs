using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateBlockGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateBlockGroup, Either<BaseError, BlockGroupViewModel>>
{
    public async Task<Either<BaseError, BlockGroupViewModel>> Handle(
        CreateBlockGroup request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, BlockGroup> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistBlockGroup(dbContext, profile));
    }

    private static async Task<BlockGroupViewModel> PersistBlockGroup(TvContext dbContext, BlockGroup blockGroup)
    {
        await dbContext.BlockGroups.AddAsync(blockGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(blockGroup);
    }

    private static Task<Validation<BaseError, BlockGroup>> Validate(TvContext dbContext, CreateBlockGroup request) =>
        ValidateName(dbContext, request).MapT(name => new BlockGroup { Name = name, Blocks = [] });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateBlockGroup createBlockGroup)
    {
        Validation<BaseError, string> result1 = createBlockGroup.NotEmpty(c => c.Name)
            .Bind(_ => createBlockGroup.NotLongerThan(50)(c => c.Name));

        bool duplicateName = await dbContext.BlockGroups
            .AnyAsync(bg => bg.Name == createBlockGroup.Name);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("Block group name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => createBlockGroup.Name);
    }
}
