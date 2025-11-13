using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Libraries;

public class MoveLocalLibraryPathHandler : IRequestHandler<MoveLocalLibraryPath, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ILanguageCodeService _languageCodeService;
    private readonly ILogger<MoveLocalLibraryPathHandler> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public MoveLocalLibraryPathHandler(
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILanguageCodeService languageCodeService,
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<MoveLocalLibraryPathHandler> logger)
    {
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _languageCodeService = languageCodeService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        MoveLocalLibraryPath request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(parameters => MovePath(dbContext, parameters, cancellationToken));
    }

    private async Task<Unit> MovePath(TvContext dbContext, Parameters parameters, CancellationToken cancellationToken)
    {
        LibraryPath path = parameters.LibraryPath;
        LocalLibrary newLibrary = parameters.Library;

        path.LibraryId = newLibrary.Id;
        if (await dbContext.SaveChangesAsync(cancellationToken) > 0)
        {
            List<int> ids = await dbContext.Connection.QueryAsync<int>(
                    @"SELECT MediaItem.Id FROM MediaItem WHERE LibraryPathId = @LibraryPathId",
                    new { LibraryPathId = path.Id })
                .Map(result => result.ToList());

            foreach (int id in ids)
            {
                Option<MediaItem> maybeMediaItem = await _searchRepository.GetItemToIndex(id, cancellationToken);
                foreach (MediaItem mediaItem in maybeMediaItem)
                {
                    _logger.LogInformation("Moving item at {Path}", await GetPath(dbContext, mediaItem));
                    await _searchIndex.UpdateItems(
                        _searchRepository,
                        _fallbackMetadataProvider,
                        _languageCodeService,
                        [mediaItem]);
                }
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        MoveLocalLibraryPath request,
        CancellationToken cancellationToken) =>
        (await LibraryPathMustExist(dbContext, request, cancellationToken),
            await LocalLibraryMustExist(dbContext, request, cancellationToken))
        .Apply((libraryPath, localLibrary) => new Parameters(libraryPath, localLibrary));

    private static Task<Validation<BaseError, LibraryPath>> LibraryPathMustExist(
        TvContext dbContext,
        MoveLocalLibraryPath request,
        CancellationToken cancellationToken) =>
        dbContext.LibraryPaths
            .Include(lp => lp.Library)
            .SelectOneAsync(c => c.Id, c => c.Id == request.LibraryPathId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("LibraryPath does not exist."));

    private static Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(
        TvContext dbContext,
        MoveLocalLibraryPath request,
        CancellationToken cancellationToken) =>
        dbContext.LocalLibraries
            .Include(ll => ll.Paths)
            .SelectOneAsync(a => a.Id, a => a.Id == request.TargetLibraryId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("LocalLibrary does not exist"));

    private static async Task<string> GetPath(TvContext dbContext, MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie => await dbContext.Connection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.MovieId = @Id",
                new { mediaItem.Id }),
            Episode => await dbContext.Connection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.EpisodeId = @Id",
                new { mediaItem.Id }),
            MusicVideo => await dbContext.Connection.QuerySingleAsync<string>(
                @"SELECT Path FROM MediaFile
                      INNER JOIN MediaVersion MV on MediaFile.MediaVersionId = MV.Id
                      WHERE MV.MusicVideoId = @Id",
                new { mediaItem.Id }),
            _ => null
        };

    private sealed record Parameters(LibraryPath LibraryPath, LocalLibrary Library);
}
