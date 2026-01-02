using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Filler;

public class CreateFillerPresetHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateFillerPreset, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(CreateFillerPreset request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FillerPreset> validation = await Validate(dbContext, request);
        return await validation.Apply(fp => Persist(dbContext, fp, cancellationToken));
    }

    private static async Task<Unit> Persist(
        TvContext dbContext,
        FillerPreset fillerPreset,
        CancellationToken cancellationToken)
    {
        await dbContext.FillerPresets.AddAsync(fillerPreset, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }

    private static Task<Validation<BaseError, FillerPreset>> Validate(
        TvContext dbContext,
        CreateFillerPreset request) =>
        ValidateName(dbContext, request).MapT(name => new FillerPreset
        {
            Name = name,
            FillerKind = request.FillerKind,
            FillerMode = request.FillerMode,
            Duration = request.Duration,
            Count = request.Count,
            PadToNearestMinute = request.PadToNearestMinute,
            AllowWatermarks = request.AllowWatermarks,
            CollectionType = request.CollectionType,
            CollectionId = request.CollectionId,
            MediaItemId = request.MediaItemId,
            MultiCollectionId = request.MultiCollectionId,
            SmartCollectionId = request.SmartCollectionId,
            PlaylistId = request.PlaylistId,
            Expression = request.FillerKind is FillerKind.MidRoll ? request.Expression : null,
            UseChaptersAsMediaItems =
                request.FillerKind is not FillerKind.Fallback && request.UseChaptersAsMediaItems
        });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateFillerPreset request)
    {
        Validation<BaseError, string> result1 = request.NotEmpty(fp => fp.Name)
            .Bind(_ => request.NotLongerThan(50)(fp => fp.Name));

        bool duplicateName = await dbContext.FillerPresets
            .AnyAsync(fp => fp.Name == request.Name);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("Filler preset name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => request.Name);
    }
}
