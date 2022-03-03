using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Watermarks;

public class DeleteWatermarkHandler : MediatR.IRequestHandler<DeleteWatermark, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, ChannelWatermark> validation = await WatermarkMustExist(dbContext, request);
        return await validation.Apply(p => DoDeletion(dbContext, p));
    }

    private static async Task<Unit> DoDeletion(TvContext dbContext, ChannelWatermark watermark)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"UPDATE Channel SET WatermarkId = NULL WHERE WatermarkId = {watermark.Id}");
        dbContext.ChannelWatermarks.Remove(watermark);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, ChannelWatermark>> WatermarkMustExist(
        TvContext dbContext,
        DeleteWatermark request) =>
        dbContext.ChannelWatermarks
            .SelectOneAsync(p => p.Id, p => p.Id == request.WatermarkId)
            .Map(o => o.ToValidation<BaseError>($"Watermark {request.WatermarkId} does not exist"));
}