using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Filler;

public class UpdateFillerPresetHandler : IRequestHandler<UpdateFillerPreset, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateFillerPresetHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(UpdateFillerPreset request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FillerPreset> validation = await FillerPresetMustExist(dbContext, request);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request));
    }

    private static async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        FillerPreset existing,
        UpdateFillerPreset request)
    {
        existing.Name = request.Name;
        existing.FillerKind = request.FillerKind;
        existing.FillerMode = request.FillerMode;
        existing.Duration = request.Duration;
        existing.Count = request.Count;
        existing.PadToNearestMinute = request.PadToNearestMinute;
        existing.AllowWatermarks = request.AllowWatermarks;
        existing.CollectionType = request.CollectionType;
        existing.CollectionId = request.CollectionId;
        existing.MediaItemId = request.MediaItemId;
        existing.MultiCollectionId = request.MultiCollectionId;
        existing.SmartCollectionId = request.SmartCollectionId;

        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }

    private static Task<Validation<BaseError, FillerPreset>> FillerPresetMustExist(
        TvContext dbContext,
        UpdateFillerPreset request) =>
        dbContext.FillerPresets
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.Id)
            .Map(o => o.ToValidation<BaseError>("FillerPreset does not exist"));
}
