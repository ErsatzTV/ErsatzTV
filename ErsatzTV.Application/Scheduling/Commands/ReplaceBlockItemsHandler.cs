using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ReplaceBlockItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ReplaceBlockItems, Either<BaseError, List<BlockItemViewModel>>>
{
    public async Task<Either<BaseError, List<BlockItemViewModel>>> Handle(
        ReplaceBlockItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Block> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => Persist(dbContext, request, ps));
    }

    private static async Task<List<BlockItemViewModel>> Persist(
        TvContext dbContext,
        ReplaceBlockItems request,
        Block block)
    {
        block.Name = request.Name;
        block.Minutes = request.Minutes;
        block.StopScheduling = request.StopScheduling;
        block.DateUpdated = DateTime.UtcNow;

        dbContext.RemoveRange(block.Items);
        block.Items = request.Items.Map(i => BuildItem(block, i.Index, i)).ToList();

        await dbContext.SaveChangesAsync();

        // TODO: refresh any playouts that use this schedule
        // foreach (Playout playout in programSchedule.Playouts)
        // {
        //     await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh));
        // }

        return block.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static BlockItem BuildItem(Block block, int index, ReplaceBlockItem item) =>
        new()
        {
            BlockId = block.Id,
            Index = index,
            CollectionType = item.CollectionType,
            CollectionId = item.CollectionId,
            MultiCollectionId = item.MultiCollectionId,
            SmartCollectionId = item.SmartCollectionId,
            MediaItemId = item.MediaItemId,
            PlaybackOrder = item.PlaybackOrder,
            IncludeInProgramGuide = item.IncludeInProgramGuide
        };

    private static Task<Validation<BaseError, Block>> Validate(TvContext dbContext, ReplaceBlockItems request) =>
        BlockMustExist(dbContext, request.BlockId)
            .BindT(block => MinutesMustBeValid(request, block))
            .BindT(block => CollectionTypesMustBeValid(request, block));

    private static Task<Validation<BaseError, Block>> BlockMustExist(TvContext dbContext, int blockId) =>
        dbContext.Blocks
            .Include(b => b.Items)
            .SelectOneAsync(b => b.Id, b => b.Id == blockId)
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
            case ProgramScheduleItemCollectionType.Collection:
                if (item.CollectionId is null)
                {
                    return BaseError.New("[Collection] is required for collection type 'Collection'");
                }

                break;
            case ProgramScheduleItemCollectionType.TelevisionShow:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionShow'");
                }

                break;
            case ProgramScheduleItemCollectionType.TelevisionSeason:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionSeason'");
                }

                break;
            case ProgramScheduleItemCollectionType.Artist:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'Artist'");
                }

                break;
            case ProgramScheduleItemCollectionType.MultiCollection:
                if (item.MultiCollectionId is null)
                {
                    return BaseError.New("[MultiCollection] is required for collection type 'MultiCollection'");
                }

                break;
            case ProgramScheduleItemCollectionType.SmartCollection:
                if (item.SmartCollectionId is null)
                {
                    return BaseError.New("[SmartCollection] is required for collection type 'SmartCollection'");
                }

                break;
            case ProgramScheduleItemCollectionType.FakeCollection:
            default:
                return BaseError.New("[CollectionType] is invalid");
        }

        return block;
    }
}
