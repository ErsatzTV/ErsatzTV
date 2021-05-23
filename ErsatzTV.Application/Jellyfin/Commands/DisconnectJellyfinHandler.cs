using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class DisconnectJellyfinHandler : MediatR.IRequestHandler<DisconnectJellyfin, Either<BaseError, Unit>>
    {
        private readonly IEntityLocker _entityLocker;
        private readonly IJellyfinSecretStore _jellyfinSecretStore;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ISearchIndex _searchIndex;

        public DisconnectJellyfinHandler(
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinSecretStore jellyfinSecretStore,
            IEntityLocker entityLocker,
            ISearchIndex searchIndex)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _jellyfinSecretStore = jellyfinSecretStore;
            _entityLocker = entityLocker;
            _searchIndex = searchIndex;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            DisconnectJellyfin request,
            CancellationToken cancellationToken)
        {
            List<int> ids = await _mediaSourceRepository.DeleteAllJellyfin();
            await _searchIndex.RemoveItems(ids);
            await _jellyfinSecretStore.DeleteAll();
            _entityLocker.UnlockRemoteMediaSource<JellyfinMediaSource>();

            return Unit.Default;
        }
    }
}
