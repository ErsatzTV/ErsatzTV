using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Plex.Commands
{
    public class SignOutOfPlexHandler : MediatR.IRequestHandler<SignOutOfPlex, Either<BaseError, Unit>>
    {
        private readonly IEntityLocker _entityLocker;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlexSecretStore _plexSecretStore;

        public SignOutOfPlexHandler(
            IMediaSourceRepository mediaSourceRepository,
            IPlexSecretStore plexSecretStore,
            IEntityLocker entityLocker)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _plexSecretStore = plexSecretStore;
            _entityLocker = entityLocker;
        }

        public async Task<Either<BaseError, Unit>> Handle(SignOutOfPlex request, CancellationToken cancellationToken)
        {
            await _mediaSourceRepository.DeleteAllPlex();
            await _plexSecretStore.DeleteAll();
            _entityLocker.UnlockPlex();

            return Unit.Default;
        }
    }
}
