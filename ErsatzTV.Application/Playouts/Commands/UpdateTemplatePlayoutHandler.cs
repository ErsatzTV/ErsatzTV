﻿using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class
    UpdateTemplatePlayoutHandler : IRequestHandler<UpdateYamlPlayout,
    Either<BaseError, PlayoutNameViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public UpdateTemplatePlayoutHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _dbContextFactory = dbContextFactory;
        _workerChannel = workerChannel;
    }

    public async Task<Either<BaseError, PlayoutNameViewModel>> Handle(
        UpdateYamlPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
    }

    private async Task<PlayoutNameViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        UpdateYamlPlayout request,
        Playout playout)
    {
        playout.TemplateFile = request.TemplateFile;

        if (await dbContext.SaveChangesAsync() > 0)
        {
            await _workerChannel.WriteAsync(new RefreshChannelData(playout.Channel.Number));
        }

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

    private static Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        UpdateYamlPlayout request) =>
        PlayoutMustExist(dbContext, request);

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        UpdateYamlPlayout updatePlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == updatePlayout.PlayoutId)
            .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
}
