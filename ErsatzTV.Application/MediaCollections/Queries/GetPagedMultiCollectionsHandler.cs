using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetPagedMultiCollectionsHandler : IRequestHandler<GetPagedMultiCollections, PagedMultiCollectionsViewModel>
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetPagedMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<PagedMultiCollectionsViewModel> Handle(
            GetPagedMultiCollections request,
            CancellationToken cancellationToken)
        {
            int count = await _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM Collection");

            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            List<MultiCollectionViewModel> page = await dbContext.MultiCollections.FromSqlRaw(
                    @"SELECT * FROM MultiCollection
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                    request.PageSize,
                    request.PageNum * request.PageSize)
                .Include(mc => mc.MultiCollectionItems)
                .ThenInclude(i => i.Collection)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new PagedMultiCollectionsViewModel(count, page);
        }
    }
}
