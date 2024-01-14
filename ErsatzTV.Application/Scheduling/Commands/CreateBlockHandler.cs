using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateBlockHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateBlock, Either<BaseError, BlockViewModel>>
{
    public async Task<Either<BaseError, BlockViewModel>> Handle(
        CreateBlock request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Block> validation = await Validate(request);
        return await validation.Apply(profile => PersistBlock(dbContext, profile));
    }

    private static async Task<BlockViewModel> PersistBlock(TvContext dbContext, Block block)
    {
        await dbContext.Blocks.AddAsync(block);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(block);
    }

    private static Task<Validation<BaseError, Block>> Validate(CreateBlock request) =>
        Task.FromResult(
            ValidateName(request).Map(
                name => new Block
                {
                    BlockGroupId = request.BlockGroupId,
                    Name = name,
                    Minutes = 30
                }));

    private static Validation<BaseError, string> ValidateName(CreateBlock createBlock) =>
        createBlock.NotEmpty(x => x.Name)
            .Bind(_ => createBlock.NotLongerThan(50)(x => x.Name));
}
