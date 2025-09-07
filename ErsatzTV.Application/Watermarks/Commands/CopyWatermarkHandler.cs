﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static ErsatzTV.Application.Watermarks.Mapper;

namespace ErsatzTV.Application.Watermarks;

public class CopyWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    : IRequestHandler<CopyWatermark, Either<BaseError, WatermarkViewModel>>
{
    public async Task<Either<BaseError, WatermarkViewModel>> Handle(
        CopyWatermark request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, CopyWatermarkParameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(c => PerformCopy(dbContext, c, cancellationToken));
    }

    private async Task<WatermarkViewModel> PerformCopy(
        TvContext dbContext,
        CopyWatermarkParameters parameters,
        CancellationToken cancellationToken)
    {
        PropertyValues values = dbContext.Entry(parameters.Watermark).CurrentValues.Clone();
        values["Id"] = 0;

        var clone = new ChannelWatermark();
        await dbContext.AddAsync(clone, cancellationToken);
        dbContext.Entry(clone).CurrentValues.SetValues(values);
        clone.Name = parameters.Name;

        await dbContext.SaveChangesAsync(cancellationToken);

        searchTargets.SearchTargetsChanged();

        return ProjectToViewModel(clone);
    }

    private static async Task<Validation<BaseError, CopyWatermarkParameters>> Validate(
        TvContext dbContext,
        CopyWatermark request,
        CancellationToken cancellationToken) =>
        (ValidateName(request), await WatermarkMustExist(dbContext, request, cancellationToken))
        .Apply((name, watermark) => new CopyWatermarkParameters(name, watermark));

    private static Validation<BaseError, string> ValidateName(CopyWatermark request) =>
        request.NotEmpty(x => x.Name)
            .Bind(_ => request.NotLongerThan(50)(x => x.Name));

    private static Task<Validation<BaseError, ChannelWatermark>> WatermarkMustExist(
        TvContext dbContext,
        CopyWatermark copyWatermark,
        CancellationToken cancellationToken) =>
        dbContext.ChannelWatermarks
            .SelectOneAsync(wm => wm.Id, wm => wm.Id == copyWatermark.WatermarkId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>($"Watermark {copyWatermark.WatermarkId} does not exist."));

    private sealed record CopyWatermarkParameters(string Name, ChannelWatermark Watermark);
}
