using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Emby.Commands
{
    public class DisconnectEmbyHandler : MediatR.IRequestHandler<DisconnectEmby, Either<BaseError, Unit>>
    {
        private readonly IEmbySecretStore _embySecretStore;
        private readonly IEntityLocker _entityLocker;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ISearchIndex _searchIndex;

        public DisconnectEmbyHandler(
            IMediaSourceRepository mediaSourceRepository,
            IEmbySecretStore embySecretStore,
            IEntityLocker entityLocker,
            ISearchIndex searchIndex)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _embySecretStore = embySecretStore;
            _entityLocker = entityLocker;
            _searchIndex = searchIndex;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            DisconnectEmby request,
            CancellationToken cancellationToken)
        {
            List<int> ids = await _mediaSourceRepository.DeleteAllEmby();
            await _searchIndex.RemoveItems(ids);
            _searchIndex.Commit();
            await _embySecretStore.DeleteAll();
            _entityLocker.UnlockRemoteMediaSource<EmbyMediaSource>();

            return Unit.Default;
        }
    }
}
