using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteTraktListHandler : TraktCommandBase, IRequestHandler<DeleteTraktList, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public DeleteTraktListHandler(
        ITraktApiClient traktApiClient,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<DeleteTraktListHandler> logger,
        IEntityLocker entityLocker)
        : base(traktApiClient, searchRepository, searchIndex, logger)
    {
        _searchRepository = searchRepository;
        _searchIndex = searchIndex;
        _dbContextFactory = dbContextFactory;
        _entityLocker = entityLocker;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteTraktList request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            Validation<BaseError, TraktList> validation = await TraktListMustExist(dbContext, request.TraktListId);
            return await LanguageExtensions.Apply(validation, c => DoDeletion(dbContext, c));
        }
        finally
        {
            _entityLocker.UnlockTrakt();
        }
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, TraktList traktList)
    {
        var mediaItemIds = traktList.Items.Bind(i => Optional(i.MediaItemId)).ToList();

        dbContext.TraktLists.Remove(traktList);
        if (await dbContext.SaveChangesAsync() > 0)
        {
            foreach (int mediaItemId in mediaItemIds)
            {
                foreach (MediaItem mediaItem in await _searchRepository.GetItemToIndex(mediaItemId))
                {
                    await _searchIndex.UpdateItems(_searchRepository, new[] { mediaItem }.ToList());
                }
            }
        }

        _searchIndex.Commit();

        return Unit.Default;
    }
}
