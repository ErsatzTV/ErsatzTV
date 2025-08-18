using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class UpdateDecoHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateDeco, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(UpdateDeco request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Deco> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request));
    }

    private static async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        Deco existing,
        UpdateDeco request)
    {
        existing.Name = request.Name;

        bool hasWatermark = request.WatermarkMode is (DecoMode.Override or DecoMode.Merge);

        // watermark
        existing.WatermarkMode = request.WatermarkMode;
        existing.UseWatermarkDuringFiller = hasWatermark && request.UseWatermarkDuringFiller;

        if (hasWatermark)
        {
            // this is different than schedule item/playout item because we have to merge watermark ids
            IEnumerable<int> toAdd =
                request.WatermarkIds.Where(id => existing.DecoWatermarks.All(wm => wm.WatermarkId != id));
            IEnumerable<DecoWatermark> toRemove =
                existing.DecoWatermarks.Where(wm => !request.WatermarkIds.Contains(wm.WatermarkId));
            existing.DecoWatermarks.RemoveAll(toRemove.Contains);
            foreach (int watermarkId in toAdd)
            {
                existing.DecoWatermarks.Add(
                    new DecoWatermark
                    {
                        DecoId = existing.Id,
                        WatermarkId = watermarkId
                    });
            }
        }
        else
        {
            existing.DecoWatermarks.Clear();
        }

        // default filler
        existing.DefaultFillerMode = request.DefaultFillerMode;
        existing.DefaultFillerCollectionType = request.DefaultFillerCollectionType;
        existing.DefaultFillerCollectionId = null;
        existing.DefaultFillerMediaItemId = null;
        existing.DefaultFillerMultiCollectionId = null;
        existing.DefaultFillerSmartCollectionId = null;
        if (request.DefaultFillerMode is DecoMode.Override)
        {
            switch (request.DefaultFillerCollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    existing.DefaultFillerCollectionId = request.DefaultFillerCollectionId;
                    break;
                case ProgramScheduleItemCollectionType.MultiCollection:
                    existing.DefaultFillerMultiCollectionId = request.DefaultFillerMultiCollectionId;
                    break;
                case ProgramScheduleItemCollectionType.SmartCollection:
                    existing.DefaultFillerSmartCollectionId = request.DefaultFillerSmartCollectionId;
                    break;
                default:
                    existing.DefaultFillerMediaItemId = request.DefaultFillerMediaItemId;
                    break;
            }
        }

        existing.DefaultFillerTrimToFit = request.DefaultFillerTrimToFit;

        // dead air fallback
        existing.DeadAirFallbackMode = request.DeadAirFallbackMode;
        existing.DeadAirFallbackCollectionType = request.DeadAirFallbackCollectionType;
        existing.DeadAirFallbackCollectionId = null;
        existing.DeadAirFallbackMediaItemId = null;
        existing.DeadAirFallbackMultiCollectionId = null;
        existing.DeadAirFallbackSmartCollectionId = null;
        if (request.DeadAirFallbackMode is DecoMode.Override)
        {
            switch (request.DeadAirFallbackCollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    existing.DeadAirFallbackCollectionId = request.DeadAirFallbackCollectionId;
                    break;
                case ProgramScheduleItemCollectionType.MultiCollection:
                    existing.DeadAirFallbackMultiCollectionId = request.DeadAirFallbackMultiCollectionId;
                    break;
                case ProgramScheduleItemCollectionType.SmartCollection:
                    existing.DeadAirFallbackSmartCollectionId = request.DeadAirFallbackSmartCollectionId;
                    break;
                default:
                    existing.DeadAirFallbackMediaItemId = request.DeadAirFallbackMediaItemId;
                    break;
            }
        }

        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Deco>> Validate(TvContext dbContext, UpdateDeco request) =>
        (await DecoMustExist(dbContext, request), await ValidateDecoName(dbContext, request))
        .Apply((deco, _) => deco);

    private static Task<Validation<BaseError, Deco>> DecoMustExist(
        TvContext dbContext,
        UpdateDeco request) =>
        dbContext.Decos
            .Include(d => d.DecoWatermarks)
            .SelectOneAsync(d => d.Id, d => d.Id == request.DecoId)
            .Map(o => o.ToValidation<BaseError>("Deco does not exist"));

    private static async Task<Validation<BaseError, string>> ValidateDecoName(
        TvContext dbContext,
        UpdateDeco request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Deco name \"{request.Name}\" is invalid");
        }

        Option<Deco> maybeExisting = await dbContext.Decos
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id != request.DecoId && d.DecoGroupId == request.DecoGroupId && d.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A deco named \"{request.Name}\" already exists in that deco group")
            : Success<BaseError, string>(request.Name);
    }
}
