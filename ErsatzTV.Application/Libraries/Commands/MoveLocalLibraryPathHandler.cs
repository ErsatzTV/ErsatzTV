using System.Data;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Libraries;

public class MoveLocalLibraryPathHandler : IRequestHandler<MoveLocalLibraryPath, Either<BaseError, Unit>>
{
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<MoveLocalLibraryPathHandler> _logger;

    public MoveLocalLibraryPathHandler(
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IDbContextFactory<TvContext> dbContextFactory,
        IDbConnection dbConnection,
        ILogger<MoveLocalLibraryPathHandler> logger)
    {
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _dbContextFactory = dbContextFactory;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        MoveLocalLibraryPath request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, parameters => MovePath(dbContext, parameters));
    }

    private async Task<Unit> MovePath(TvContext dbContext, Parameters parameters)
    {
        LibraryPath path = parameters.LibraryPath;
        LocalLibrary newLibrary = parameters.Library;

        path.LibraryId = newLibrary.Id;
        if (await dbContext.SaveChangesAsync() > 0)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT MediaItem.Id FROM MediaItem WHERE LibraryPathId = @LibraryPathId",
                    new { LibraryPathId = path.Id })
                .Map(result => result.ToList());

            foreach (int id in ids)
            {
                Option<MediaItem> maybeMediaItem = await _searchRepository.GetItemToIndex(id);
                foreach (MediaItem mediaItem in maybeMediaItem)
                {
                    _logger.LogInformation("Moving item at {Path}", await GetPath(mediaItem));
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { mediaItem });
                }
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        MoveLocalLibraryPath request) =>
        (await LibraryPathMustExist(dbContext, request), await LocalLibraryMustExist(dbContext, request))
        .Apply((libraryPath, localLibrary) => new Parameters(libraryPath, localLibrary));

    private static Task<Validation<BaseError, LibraryPath>> LibraryPathMustExist(
        TvContext dbContext,
        MoveLocalLibraryPath request) =>
        dbContext.LibraryPaths
            .Include(lp => lp.Library)
            .SelectOneAsync(c => c.Id, c => c.Id == request.LibraryPathId)
            .Map(o => o.ToValidation<BaseError>("LibraryPath does not exist."));

    private static Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(
        TvContext dbContext,
        MoveLocalLibraryPath request) =>
        dbContext.LocalLibraries
            .Include(ll => ll.Paths)
            .SelectOneAsync(a => a.Id, a => a.Id == request.TargetLibraryId)
            .Map(o => o.ToValidation<BaseError>("LocalLibrary does not exist"));

    private async Task<string> GetPath(MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie => await _dbConnection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.MovieId = @Id", new { mediaItem.Id }),
            Episode => await _dbConnection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.EpisodeId = @Id", new { mediaItem.Id }),
            MusicVideo => await _dbConnection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.MusicVideoId = @Id", new { mediaItem.Id }),
            _ => null
        };

    private record Parameters(LibraryPath LibraryPath, LocalLibrary Library);
}