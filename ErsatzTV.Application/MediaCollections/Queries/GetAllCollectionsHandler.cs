﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetAllCollectionsHandler : IRequestHandler<GetAllCollections, List<MediaCollectionViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetAllCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<List<MediaCollectionViewModel>> Handle(
            GetAllCollections request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Collections
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
        }
    }
}
