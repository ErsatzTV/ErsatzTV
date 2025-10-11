using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Scheduling.Mapper;

namespace ErsatzTV.Application.Scheduling;

public class CopyBlockHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CopyBlock, Either<BaseError, BlockViewModel>>
{
    public async Task<Either<BaseError, BlockViewModel>> Handle(
        CopyBlock request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            Validation<BaseError, Block> validation = await Validate(dbContext, request, cancellationToken);
            return await validation.Apply(p => PerformCopy(dbContext, p, request, cancellationToken));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<BlockViewModel> PerformCopy(
        TvContext dbContext,
        Block block,
        CopyBlock request,
        CancellationToken cancellationToken)
    {
        DetachEntity(dbContext, block);
        block.Name = request.NewBlockName;
        block.BlockGroup = null;
        block.BlockGroupId = request.NewBlockGroupId;

        foreach (BlockItem item in block.Items)
        {
            DetachEntity(dbContext, item);
            item.BlockId = 0;
            item.Block = block;

            foreach (BlockItemWatermark watermark in item.BlockItemWatermarks)
            {
                DetachEntity(dbContext, watermark);
                watermark.BlockItemId = 0;
                watermark.BlockItem = item;
            }

            foreach (BlockItemGraphicsElement graphicsElement in item.BlockItemGraphicsElements)
            {
                DetachEntity(dbContext, graphicsElement);
                graphicsElement.BlockItemId = 0;
                graphicsElement.BlockItem = item;
            }
        }

        await dbContext.Blocks.AddAsync(block, cancellationToken);
        await dbContext.BlockItems.AddRangeAsync(block.Items, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(block).Reference(b => b.BlockGroup).LoadAsync(cancellationToken);

        return ProjectToViewModel(block);
    }

    private static async Task<Validation<BaseError, Block>> Validate(
        TvContext dbContext,
        CopyBlock request,
        CancellationToken cancellationToken) =>
        (await BlockMustExist(dbContext, request, cancellationToken), await ValidateName(dbContext, request))
        .Apply((block, _) => block);

    private static Task<Validation<BaseError, Block>> BlockMustExist(
        TvContext dbContext,
        CopyBlock request,
        CancellationToken cancellationToken) =>
        dbContext.Blocks
            .AsNoTracking()
            .Include(ps => ps.Items)
            .ThenInclude(i => i.BlockItemWatermarks)
            .Include(ps => ps.Items)
            .ThenInclude(i => i.BlockItemGraphicsElements)
            .SelectOneAsync(p => p.Id, p => p.Id == request.BlockId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Block does not exist."));

    private static async Task<Validation<BaseError, string>> ValidateName(TvContext dbContext, CopyBlock request)
    {
        List<string> allNames = await dbContext.Blocks
            .Where(b => b.BlockGroupId == request.NewBlockGroupId)
            .Map(ps => ps.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = request.NotEmpty(c => c.NewBlockName)
            .Bind(_ => request.NotLongerThan(50)(c => c.NewBlockName));

        var result2 = Optional(request.NewBlockName)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("Block name must be unique within the block group.");

        return (result1, result2).Apply((_, _) => request.NewBlockName);
    }

    private static void DetachEntity<T>(TvContext db, T entity) where T : class
    {
        db.Entry(entity).State = EntityState.Detached;
        if (entity.GetType().GetProperty("Id") is not null)
        {
            entity.GetType().GetProperty("Id")!.SetValue(entity, 0);
        }
    }
}
