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
        Validation<BaseError, ChannelWatermark> validation = Validate(request);
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
                    Opacity = request.Opacity,
                    PlaceWithinSourceContent = request.PlaceWithinSourceContent
                });

    private static Validation<BaseError, string> ValidateName(CreateWatermark request) =>
        request.NotEmpty(x => x.Name)
            .Bind(_ => request.NotLongerThan(50)(x => x.Name));
}
