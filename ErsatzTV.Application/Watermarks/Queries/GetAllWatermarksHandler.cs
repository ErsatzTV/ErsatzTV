﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Watermarks.Mapper;

namespace ErsatzTV.Application.Watermarks.Queries
{
    public class GetAllWatermarksHandler : IRequestHandler<GetAllWatermarks, List<WatermarkViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetAllWatermarksHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<List<WatermarkViewModel>> Handle(
            GetAllWatermarks request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ChannelWatermarks
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
        }
    }
}
