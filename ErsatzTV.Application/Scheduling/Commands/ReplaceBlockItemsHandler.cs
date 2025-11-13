using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ReplaceBlockItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ReplaceBlockItems, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        ReplaceBlockItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Block> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(ps => Persist(dbContext, request, ps, cancellationToken));
    }

    private static async Task<Unit> Persist(
        TvContext dbContext,
        ReplaceBlockItems request,
        Block block,
        CancellationToken cancellationToken)
    {
        block.Name = request.Name;
        block.Minutes = request.Minutes;
        block.StopScheduling = request.StopScheduling;
        block.DateUpdated = DateTime.UtcNow;

        dbContext.RemoveRange(block.Items);
        block.Items = request.Items.Map(i => BuildItem(block, i.Index, i)).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);

        // TODO: refresh any playouts that use this schedule
        // foreach (Playout playout in programSchedule.Playouts)
        // {
        //     await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh));
        // }

        return Unit.Default;
    }

    private static BlockItem BuildItem(Block block, int index, ReplaceBlockItem item)
    {
        var result = new BlockItem
        {
            BlockId = block.Id,
            Index = index,
            CollectionType = item.CollectionType,
            CollectionId = item.CollectionId,
            MultiCollectionId = item.MultiCollectionId,
            SmartCollectionId = item.SmartCollectionId,
            MediaItemId = item.MediaItemId,
            SearchTitle = item.SearchTitle,
            SearchQuery = item.SearchQuery,
            PlaybackOrder = item.PlaybackOrder,
            IncludeInProgramGuide = item.IncludeInProgramGuide,
            DisableWatermarks = item.DisableWatermarks,
            BlockItemWatermarks = [],
            BlockItemGraphicsElements = []
        };

        foreach (int watermarkId in item.WatermarkIds)
        {
            result.BlockItemWatermarks ??= [];
            result.BlockItemWatermarks.Add(
                new BlockItemWatermark
                {
                    BlockItem = result,
                    WatermarkId = watermarkId
                });
        }

        foreach (int graphicsElementId in item.GraphicsElementIds)
        {
            result.BlockItemGraphicsElements ??= [];
            result.BlockItemGraphicsElements.Add(
                new BlockItemGraphicsElement
                {
                    BlockItem = result,
                    GraphicsElementId = graphicsElementId
                });
        }

        return result;
    }

    private static Task<Validation<BaseError, Block>> Validate(
        TvContext dbContext,
        ReplaceBlockItems request,
        CancellationToken cancellationToken) =>
        BlockMustExist(dbContext, request.BlockId, cancellationToken)
            .BindT(block => MinutesMustBeValid(request, block))
            .BindT(block => BlockNameMustBeValid(dbContext, block, request))
            .BindT(block => CollectionTypesMustBeValid(request, block));

    private static Task<Validation<BaseError, Block>> BlockMustExist(
        TvContext dbContext,
        int blockId,
        CancellationToken cancellationToken) =>
        dbContext.Blocks
            .Include(b => b.Items)
            .ThenInclude(i => i.BlockItemWatermarks)
            .ThenInclude(wm => wm.Watermark)
            .Include(b => b.Items)
            .ThenInclude(i => i.BlockItemGraphicsElements)
            .ThenInclude(ge => ge.GraphicsElement)
            .SelectOneAsync(b => b.Id, b => b.Id == blockId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("[BlockId] does not exist."));

    private static Validation<BaseError, Block> MinutesMustBeValid(ReplaceBlockItems request, Block block) =>
        Optional(block)
            .Filter(_ => request.Minutes > 0 && request.Minutes % 5 == 0 && request.Minutes <= 24 * 60)
            .ToValidation<BaseError>("Block duration must be between 5 minutes and 24 hours");

    private static Validation<BaseError, Block> CollectionTypesMustBeValid(ReplaceBlockItems request, Block block) =>
        request.Items.Map(item => CollectionTypeMustBeValid(item, block)).Sequence().Map(_ => block);

    private static Validation<BaseError, Block> CollectionTypeMustBeValid(ReplaceBlockItem item, Block block)
    {
        switch (item.CollectionType)
        {
            case CollectionType.Collection:
                if (item.CollectionId is null)
                {
                    return BaseError.New("[Collection] is required for collection type 'Collection'");
                }

                break;
            case CollectionType.TelevisionShow:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionShow'");
                }

                break;
            case CollectionType.TelevisionSeason:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionSeason'");
                }

                break;
            case CollectionType.Artist:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'Artist'");
                }

                break;
            case CollectionType.MultiCollection:
                if (item.MultiCollectionId is null)
                {
                    return BaseError.New("[MultiCollection] is required for collection type 'MultiCollection'");
                }

                break;
            case CollectionType.SmartCollection:
                if (item.SmartCollectionId is null)
                {
                    return BaseError.New("[SmartCollection] is required for collection type 'SmartCollection'");
                }

                break;
            case CollectionType.SearchQuery:
                if (string.IsNullOrWhiteSpace(item.SearchQuery))
                {
                    return BaseError.New("[SearchQuery] is required for collection type 'SearchQuery'");
                }

                break;
            case CollectionType.FakeCollection:
            default:
                return BaseError.New("[CollectionType] is invalid");
        }

        return block;
    }

    private static async Task<Validation<BaseError, Block>> BlockNameMustBeValid(
        TvContext dbContext,
        Block block,
        ReplaceBlockItems request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Block name \"{request.Name}\" is invalid");
        }

        Option<Block> maybeExisting = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id != request.BlockId && d.BlockGroupId == request.BlockGroupId && d.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A block named \"{request.Name}\" already exists in that block group")
            : Success<BaseError, Block>(block);
    }
}
