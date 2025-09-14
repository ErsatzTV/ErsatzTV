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
        Validation<BaseError, Deco> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request, cancellationToken));
    }

    private static async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        Deco existing,
        UpdateDeco request,
        CancellationToken cancellationToken)
    {
        existing.Name = request.Name;

        // watermark
        bool hasWatermark = request.WatermarkMode is (DecoMode.Override or DecoMode.Merge);
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

        // graphics elements
        bool hasGraphicsElements = request.GraphicsElementsMode is (DecoMode.Override or DecoMode.Merge);
        existing.GraphicsElementsMode = request.GraphicsElementsMode;
        existing.UseGraphicsElementsDuringFiller = hasGraphicsElements && request.UseGraphicsElementsDuringFiller;

        if (hasGraphicsElements)
        {
            // this is different than schedule item/playout item because we have to merge graphics element ids
            IEnumerable<int> toAdd =
                request.GraphicsElementIds.Where(id => existing.DecoGraphicsElements.All(ge => ge.GraphicsElementId != id));
            IEnumerable<DecoGraphicsElement> toRemove =
                existing.DecoGraphicsElements.Where(ge => !request.GraphicsElementIds.Contains(ge.GraphicsElementId));
            existing.DecoGraphicsElements.RemoveAll(toRemove.Contains);
            foreach (int graphicsElementId in toAdd)
            {
                existing.DecoGraphicsElements.Add(
                    new DecoGraphicsElement
                    {
                        DecoId = existing.Id,
                        GraphicsElementId = graphicsElementId
                    });
            }
        }
        else
        {
            existing.DecoGraphicsElements.Clear();
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Deco>> Validate(
        TvContext dbContext,
        UpdateDeco request,
        CancellationToken cancellationToken) =>
        (await DecoMustExist(dbContext, request, cancellationToken), await ValidateDecoName(dbContext, request))
        .Apply((deco, _) => deco);

    private static Task<Validation<BaseError, Deco>> DecoMustExist(
        TvContext dbContext,
        UpdateDeco request,
        CancellationToken cancellationToken) =>
        dbContext.Decos
            .Include(d => d.DecoWatermarks)
            .Include(d => d.DecoGraphicsElements)
            .SelectOneAsync(d => d.Id, d => d.Id == request.DecoId, cancellationToken)
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
