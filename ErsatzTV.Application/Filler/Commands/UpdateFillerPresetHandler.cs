using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Filler;

public class UpdateFillerPresetHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateFillerPreset, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(UpdateFillerPreset request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FillerPreset> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request, cancellationToken));
    }

    private static async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        FillerPreset existing,
        UpdateFillerPreset request,
        CancellationToken cancellationToken)
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
        existing.PlaylistId = request.PlaylistId;
        existing.Expression = request.FillerKind is FillerKind.MidRoll ? request.Expression : null;
        existing.UseChaptersAsMediaItems =
            request.FillerKind is not FillerKind.Fallback && request.UseChaptersAsMediaItems;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, FillerPreset>> Validate(
        TvContext dbContext,
        UpdateFillerPreset request,
        CancellationToken cancellationToken) =>
        (await FillerPresetMustExist(dbContext, request, cancellationToken), await ValidateName(dbContext, request))
        .Apply((collectionToUpdate, _) => collectionToUpdate);

    private static Task<Validation<BaseError, FillerPreset>> FillerPresetMustExist(
        TvContext dbContext,
        UpdateFillerPreset request,
        CancellationToken cancellationToken) =>
        dbContext.FillerPresets
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.Id, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Filler preset does not exist"));

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        UpdateFillerPreset request)
    {
        Validation<BaseError, string> result1 = request.NotEmpty(fp => fp.Name)
            .Bind(_ => request.NotLongerThan(50)(fp => fp.Name));

        bool duplicateName = await dbContext.FillerPresets
            .AnyAsync(c => c.Id != request.Id && c.Name == request.Name);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("Filler preset name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => request.Name);
    }
}
