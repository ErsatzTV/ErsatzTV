using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Maintenance;

public class
    DeleteItemsFromDatabaseHandler : MediatR.IRequestHandler<DeleteItemsFromDatabase, Either<BaseError, Unit>>
{
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ISearchIndex _searchIndex;

    public DeleteItemsFromDatabaseHandler(
        IMediaItemRepository mediaItemRepository,
        ISearchIndex searchIndex)
    {
        _mediaItemRepository = mediaItemRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteItemsFromDatabase request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> deleteResult = await _mediaItemRepository.DeleteItems(request.MediaItemIds);
        if (deleteResult.IsRight)
        {
            await _searchIndex.RemoveItems(request.MediaItemIds);
            _searchIndex.Commit();
        }

        return deleteResult;
    }
}