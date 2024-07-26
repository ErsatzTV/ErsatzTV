﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class UpdatePlayoutHandler : IRequestHandler<UpdatePlayout, Either<BaseError, PlayoutNameViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdatePlayoutHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, PlayoutNameViewModel>> Handle(
        UpdatePlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
    }

    private static async Task<PlayoutNameViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        UpdatePlayout request,
        Playout playout)
    {
        playout.DailyRebuildTime = null;

        foreach (TimeSpan dailyRebuildTime in request.DailyRebuildTime)
        {
            playout.DailyRebuildTime = dailyRebuildTime;
        }

        await dbContext.SaveChangesAsync();

        return new PlayoutNameViewModel(
            playout.Id,
            playout.ProgramSchedulePlayoutType,
            playout.Channel.Name,
            playout.Channel.Number,
            playout.Channel.ProgressMode,
            playout.ProgramSchedule?.Name ?? string.Empty,
            playout.TemplateFile,
            playout.ExternalJsonFile,
            playout.DailyRebuildTime);
    }

    private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, UpdatePlayout request) =>
        PlayoutMustExist(dbContext, request);

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        UpdatePlayout updatePlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .Include(p => p.ProgramSchedule)
            .SelectOneAsync(p => p.Id, p => p.Id == updatePlayout.PlayoutId)
            .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
}
