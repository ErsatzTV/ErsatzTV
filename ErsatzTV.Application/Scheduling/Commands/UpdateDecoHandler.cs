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

        // watermark
        existing.WatermarkMode = request.WatermarkMode;
        existing.WatermarkId = request.WatermarkMode is DecoMode.Override ? request.WatermarkId : null;
        existing.UseWatermarkDuringFiller =
            request.WatermarkMode is DecoMode.Override && request.UseWatermarkDuringFiller;

        if (request.WatermarkMode is DecoMode.Override)
        {
            // this is different than schedule item/playout item because we have to merge watermark ids
            var toAdd = request.WatermarkIds.Where(id => existing.DecoWatermarks.All(wm => wm.WatermarkId != id));
            var toRemove = existing.DecoWatermarks.Where(wm => !request.WatermarkIds.Contains(wm.WatermarkId));
            existing.DecoWatermarks.RemoveAll(toRemove.Contains);
            foreach (var watermarkId in toAdd)
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
        existing.DefaultFillerCollectionId = request.DefaultFillerMode is DecoMode.Override
            ? request.DefaultFillerCollectionId
            : null;
        existing.DefaultFillerMediaItemId = request.DefaultFillerMode is DecoMode.Override
            ? request.DefaultFillerMediaItemId
            : null;
        existing.DefaultFillerMultiCollectionId = request.DefaultFillerMode is DecoMode.Override
            ? request.DefaultFillerMultiCollectionId
            : null;
        existing.DefaultFillerSmartCollectionId = request.DefaultFillerMode is DecoMode.Override
            ? request.DefaultFillerSmartCollectionId
            : null;
        existing.DefaultFillerTrimToFit = request.DefaultFillerTrimToFit;

        // dead air fallback
        existing.DeadAirFallbackMode = request.DeadAirFallbackMode;
        existing.DeadAirFallbackCollectionType = request.DeadAirFallbackCollectionType;
        existing.DeadAirFallbackCollectionId = request.DeadAirFallbackMode is DecoMode.Override
            ? request.DeadAirFallbackCollectionId
            : null;
        existing.DeadAirFallbackMediaItemId = request.DeadAirFallbackMode is DecoMode.Override
            ? request.DeadAirFallbackMediaItemId
            : null;
        existing.DeadAirFallbackMultiCollectionId = request.DeadAirFallbackMode is DecoMode.Override
            ? request.DeadAirFallbackMultiCollectionId
            : null;
        existing.DeadAirFallbackSmartCollectionId = request.DeadAirFallbackMode is DecoMode.Override
            ? request.DeadAirFallbackSmartCollectionId
            : null;

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
