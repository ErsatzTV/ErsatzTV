using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Plex;

public class SignOutOfPlexHandler : MediatR.IRequestHandler<SignOutOfPlex, Either<BaseError, Unit>>
{
    private readonly IEntityLocker _entityLocker;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly ISearchIndex _searchIndex;

    public SignOutOfPlexHandler(
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore,
        IEntityLocker entityLocker,
        ISearchIndex searchIndex)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
        _entityLocker = entityLocker;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(SignOutOfPlex request, CancellationToken cancellationToken)
    {
        List<int> ids = await _mediaSourceRepository.DeleteAllPlex();
        await _searchIndex.RemoveItems(ids);
        _searchIndex.Commit();
        await _plexSecretStore.DeleteAll();
        _entityLocker.UnlockPlex();

        return Unit.Default;
    }
}