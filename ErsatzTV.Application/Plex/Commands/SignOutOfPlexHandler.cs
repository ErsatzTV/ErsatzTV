using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Plex.Commands
{
    public class SignOutOfPlexHandler : MediatR.IRequestHandler<SignOutOfPlex, Either<BaseError, Unit>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlexSecretStore _plexSecretStore;

        public SignOutOfPlexHandler(IMediaSourceRepository mediaSourceRepository, IPlexSecretStore plexSecretStore)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _plexSecretStore = plexSecretStore;
        }

        public async Task<Either<BaseError, Unit>> Handle(SignOutOfPlex request, CancellationToken cancellationToken)
        {
            await _mediaSourceRepository.DeleteAllPlex();
            await _plexSecretStore.DeleteAll();

            return Unit.Default;
        }
    }
}
