﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Playouts.Queries
{
    public class GetAllPlayoutsHandler : IRequestHandler<GetAllPlayouts, List<PlayoutNameViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetAllPlayoutsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<List<PlayoutNameViewModel>> Handle(
            GetAllPlayouts request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Playouts
                .Filter(p => p.Channel != null && p.ProgramSchedule != null)
                .Map(
                    p => new PlayoutNameViewModel(
                        p.Id,
                        p.Channel.Name,
                        p.Channel.Number,
                        p.ProgramSchedule.Name,
                        Optional(p.DailyRebuildTime)))
                .ToListAsync(cancellationToken);
        }
    }
}
