using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly IDbConnection _dbConnection;

        public MediaItemRepository(IDbConnection dbConnection) => _dbConnection = dbConnection;

        public Task<List<string>> GetAllLanguageCodes() =>
            _dbConnection.QueryAsync<string>(
                    @"SELECT LanguageCode FROM
                    (SELECT Language AS LanguageCode
                    FROM MediaStream WHERE Language IS NOT NULL
                    UNION ALL SELECT PreferredLanguageCode AS LanguageCode
                    FROM Channel WHERE PreferredLanguageCode IS NOT NULL)
                    GROUP BY LanguageCode
                    ORDER BY COUNT(LanguageCode) DESC")
                .Map(result => result.ToList());
    }
}
