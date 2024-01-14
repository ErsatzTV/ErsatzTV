using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateBlockGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateBlockGroup, Either<BaseError, BlockGroupViewModel>>
{
    public async Task<Either<BaseError, BlockGroupViewModel>> Handle(CreateBlockGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, BlockGroup> validation = await Validate(request);
        return await validation.Apply(profile => PersistBlockGroup(dbContext, profile));
    }
    
    private static async Task<BlockGroupViewModel> PersistBlockGroup(TvContext dbContext, BlockGroup blockGroup)
    {
        await dbContext.BlockGroups.AddAsync(blockGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(blockGroup);
    }

    private static Task<Validation<BaseError, BlockGroup>> Validate(CreateBlockGroup request) =>
        Task.FromResult(ValidateName(request).Map(name => new BlockGroup { Name = name, Blocks = [] }));
    
    private static Validation<BaseError, string> ValidateName(CreateBlockGroup createBlockGroup) =>
        createBlockGroup.NotEmpty(x => x.Name)
            .Bind(_ => createBlockGroup.NotLongerThan(50)(x => x.Name));
}
