using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Watermarks;

public class UpdateWatermarkHandler : IRequestHandler<UpdateWatermark, Either<BaseError, UpdateWatermarkResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, UpdateWatermarkResult>> Handle(
        UpdateWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ChannelWatermark> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, p => ApplyUpdateRequest(dbContext, p, request));
    }

    private static async Task<UpdateWatermarkResult> ApplyUpdateRequest(
        TvContext dbContext,
        ChannelWatermark p,
        UpdateWatermark update)
    {
        p.Name = update.Name;
        p.Image = update.ImageSource == ChannelWatermarkImageSource.Custom ? update.Image : null;
        p.Mode = update.Mode;
        p.ImageSource = update.ImageSource;
        p.Location = update.Location;
        p.Size = update.Size;
        p.WidthPercent = update.Width;
        p.HorizontalMarginPercent = update.HorizontalMargin;
        p.VerticalMarginPercent = update.VerticalMargin;
        p.FrequencyMinutes = update.FrequencyMinutes;
        p.DurationSeconds = update.DurationSeconds;
        p.Opacity = update.Opacity;
        await dbContext.SaveChangesAsync();
        return new UpdateWatermarkResult(p.Id);
    }

    private static async Task<Validation<BaseError, ChannelWatermark>> Validate(
        TvContext dbContext,
        UpdateWatermark request) =>
        (await WatermarkMustExist(dbContext, request), ValidateName(request))
        .Apply((watermark, _) => watermark);

    private static Task<Validation<BaseError, ChannelWatermark>> WatermarkMustExist(
        TvContext dbContext,
        UpdateWatermark updateWatermark) =>
        dbContext.ChannelWatermarks
            .SelectOneAsync(p => p.Id, p => p.Id == updateWatermark.Id)
            .Map(o => o.ToValidation<BaseError>("Watermark does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateWatermark updateWatermark) =>
        updateWatermark.NotEmpty(x => x.Name)
            .Bind(_ => updateWatermark.NotLongerThan(50)(x => x.Name));
}
