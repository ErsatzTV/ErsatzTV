using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class UpdateDecoHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateDeco, Either<BaseError, DecoViewModel>>
{
    public async Task<Either<BaseError, DecoViewModel>> Handle(UpdateDeco request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Deco> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request));
    }

    private static async Task<DecoViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        Deco existing,
        UpdateDeco request)
    {
        existing.Name = request.Name;
        
        // watermark
        existing.WatermarkMode = request.WatermarkMode;
        existing.WatermarkId = request.WatermarkMode is DecoMode.Override ? request.WatermarkId : null;
        
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

        return Mapper.ProjectToViewModel(existing);
    }

    private static async Task<Validation<BaseError, Deco>> Validate(TvContext dbContext, UpdateDeco request) =>
        (await DecoMustExist(dbContext, request), await ValidateDecoName(dbContext, request))
        .Apply((deco, _) => deco);

    private static Task<Validation<BaseError, Deco>> DecoMustExist(
        TvContext dbContext,
        UpdateDeco request) =>
        dbContext.Decos
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
            .FirstOrDefaultAsync(
                d => d.Id != request.DecoId && d.DecoGroupId == request.DecoGroupId && d.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A deco named \"{request.Name}\" already exists in that deco group")
            : Success<BaseError, string>(request.Name);
    }
}
