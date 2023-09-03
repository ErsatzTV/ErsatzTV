using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Filler;

public class DeleteFillerPresetHandler : IRequestHandler<DeleteFillerPreset, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteFillerPresetHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteFillerPreset request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FillerPreset> validation = await FillerPresetMustExist(dbContext, request);
        return await validation.Apply(ps => DoDeletion(dbContext, ps));
    }

    private static Task<Unit> DoDeletion(TvContext dbContext, FillerPreset fillerPreset)
    {
        dbContext.FillerPresets.Remove(fillerPreset);
        return dbContext.SaveChangesAsync().ToUnit();
    }

    private static Task<Validation<BaseError, FillerPreset>> FillerPresetMustExist(
        TvContext dbContext,
        DeleteFillerPreset request) =>
        dbContext.FillerPresets
            .SelectOneAsync(fp => fp.Id, ps => ps.Id == request.FillerPresetId)
            .Map(o => o.ToValidation<BaseError>($"FillerPreset {request.FillerPresetId} does not exist."));
}
