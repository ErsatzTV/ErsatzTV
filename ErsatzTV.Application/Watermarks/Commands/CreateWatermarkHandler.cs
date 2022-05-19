using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Watermarks;

public class CreateWatermarkHandler : IRequestHandler<CreateWatermark, Either<BaseError, CreateWatermarkResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, CreateWatermarkResult>> Handle(
        CreateWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, ChannelWatermark> validation = Validate(request);
        return await LanguageExtensions.Apply(validation, profile => PersistChannelWatermark(dbContext, profile));
    }

    private static async Task<CreateWatermarkResult> PersistChannelWatermark(
        TvContext dbContext,
        ChannelWatermark watermark)
    {
        await dbContext.ChannelWatermarks.AddAsync(watermark);
        await dbContext.SaveChangesAsync();
        return new CreateWatermarkResult(watermark.Id);
    }

    private static Validation<BaseError, ChannelWatermark> Validate(CreateWatermark request) =>
        ValidateName(request)
            .Map(
                _ => new ChannelWatermark
                {
                    Name = request.Name,
                    Image = request.ImageSource == ChannelWatermarkImageSource.Custom ? request.Image : null,
                    Mode = request.Mode,
                    ImageSource = request.ImageSource,
                    Location = request.Location,
                    Size = request.Size,
                    WidthPercent = request.Width,
                    HorizontalMarginPercent = request.HorizontalMargin,
                    VerticalMarginPercent = request.VerticalMargin,
                    FrequencyMinutes = request.FrequencyMinutes,
                    DurationSeconds = request.DurationSeconds,
                    Opacity = request.Opacity
                });

    private static Validation<BaseError, string> ValidateName(CreateWatermark request) =>
        request.NotEmpty(x => x.Name)
            .Bind(_ => request.NotLongerThan(50)(x => x.Name));
}
