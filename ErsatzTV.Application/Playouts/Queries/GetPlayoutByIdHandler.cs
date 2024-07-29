﻿using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlayoutById, Option<PlayoutNameViewModel>>
{
    public async Task<Option<PlayoutNameViewModel>> Handle(
        GetPlayoutById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.ProgramSchedule)
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId)
            .MapT(
                p => new PlayoutNameViewModel(
                    p.Id,
                    p.ProgramSchedulePlayoutType,
                    p.Channel.Name,
                    p.Channel.Number,
                    p.Channel.ProgressMode,
                    p.ProgramScheduleId == null ? string.Empty : p.ProgramSchedule.Name,
                    p.TemplateFile,
                    p.ExternalJsonFile,
                    p.DailyRebuildTime));
    }
}
