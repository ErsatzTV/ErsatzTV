using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Watermarks;

public class DeleteWatermarkHandler : IRequestHandler<DeleteWatermark, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public DeleteWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ChannelWatermark> validation = await WatermarkMustExist(dbContext, request);
        return await validation.Apply(p => DoDeletion(dbContext, p));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, ChannelWatermark watermark)
    {
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Channel SET WatermarkId = NULL WHERE WatermarkId = {watermark.Id}");
        dbContext.ChannelWatermarks.Remove(watermark);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, ChannelWatermark>> WatermarkMustExist(
        TvContext dbContext,
        DeleteWatermark request) =>
        dbContext.ChannelWatermarks
            .SelectOneAsync(p => p.Id, p => p.Id == request.WatermarkId)
            .Map(o => o.ToValidation<BaseError>($"Watermark {request.WatermarkId} does not exist"));
}
