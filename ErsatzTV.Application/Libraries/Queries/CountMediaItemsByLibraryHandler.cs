using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;

namespace ErsatzTV.Application.Libraries;

public class CountMediaItemsByLibraryHandler : IRequestHandler<CountMediaItemsByLibrary, int>
{
    private readonly IDbConnection _dbConnection;

    public CountMediaItemsByLibraryHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public Task<int> Handle(CountMediaItemsByLibrary request, CancellationToken cancellationToken) =>
        _dbConnection.QuerySingleAsync<int>(
            @"SELECT COUNT(*) FROM MediaItem
                  INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                  WHERE LP.LibraryId = @LibraryId",
            new { request.LibraryId });
}