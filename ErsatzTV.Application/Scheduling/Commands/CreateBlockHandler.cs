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
        Validation<BaseError, Block> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistBlock(dbContext, profile));
    }

    private static async Task<BlockViewModel> PersistBlock(TvContext dbContext, Block block)
    {
        await dbContext.Blocks.AddAsync(block);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(block);
    }

    private static async Task<Validation<BaseError, Block>> Validate(TvContext dbContext, CreateBlock request) =>
        await ValidateBlockName(dbContext, request).MapT(
            name => new Block
            {
                BlockGroupId = request.BlockGroupId,
                Name = name,
                Minutes = 30
            });

    private static async Task<Validation<BaseError, string>> ValidateBlockName(
        TvContext dbContext,
        CreateBlock request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Block name \"{request.Name}\" is invalid");
        }

        Option<Block> maybeExisting = await dbContext.Blocks
            .FirstOrDefaultAsync(r => r.BlockGroupId == request.BlockGroupId && r.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A block named \"{request.Name}\" already exists in that block group")
            : request.Name;
    }
}
