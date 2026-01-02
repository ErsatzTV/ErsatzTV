using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Watermarks;

public class CreateWatermarkHandler : IRequestHandler<CreateWatermark, Either<BaseError, CreateWatermarkResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public CreateWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, CreateWatermarkResult>> Handle(
        CreateWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ChannelWatermark> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistChannelWatermark(dbContext, profile));
    }

    private async Task<CreateWatermarkResult> PersistChannelWatermark(
        TvContext dbContext,
        ChannelWatermark watermark)
    {
        await dbContext.ChannelWatermarks.AddAsync(watermark);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return new CreateWatermarkResult(watermark.Id);
    }

    private static async Task<Validation<BaseError, ChannelWatermark>> Validate(
        TvContext dbContext,
        CreateWatermark request) =>
        await ValidateName(dbContext, request)
            .Map(_ =>
            {
                var watermark = new ChannelWatermark
                {
                    Name = request.Name,
                    Image = null,
                    OriginalContentType = null,
                    Mode = request.Mode,
                    ImageSource = request.ImageSource,
                    Location = request.Location,
                    Size = request.Size,
                    WidthPercent = request.Width,
                    HorizontalMarginPercent = request.HorizontalMargin,
                    VerticalMarginPercent = request.VerticalMargin,
                    FrequencyMinutes = request.FrequencyMinutes,
                    DurationSeconds = request.DurationSeconds,
                    Opacity = request.Opacity,
                    PlaceWithinSourceContent = request.PlaceWithinSourceContent,
                    OpacityExpression = request.OpacityExpression,
                    ZIndex = request.ZIndex
                };

                if (request.ImageSource == ChannelWatermarkImageSource.Custom)
                {
                    watermark.Image = request.Image?.Path;
                    watermark.OriginalContentType = request.Image?.ContentType;
                }

                return watermark;
            });

    private static async Task<Validation<BaseError, string>> ValidateName(TvContext dbContext, CreateWatermark request)
    {
        Validation<BaseError, string> result1 = request.NotEmpty(c => c.Name)
            .Bind(_ => request.NotLongerThan(50)(c => c.Name));

        bool duplicateName = await dbContext.ChannelWatermarks
            .AnyAsync(wm => wm.Name == request.Name);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("ChannelWatermark name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => request.Name);
    }
}
