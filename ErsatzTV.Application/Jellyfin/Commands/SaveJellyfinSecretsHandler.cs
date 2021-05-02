using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class SaveJellyfinSecretsHandler : MediatR.IRequestHandler<SaveJellyfinSecrets, Either<BaseError, Unit>>
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly IJellyfinSecretStore _jellyfinSecretStore;

        public SaveJellyfinSecretsHandler(
            IJellyfinSecretStore jellyfinSecretStore,
            IJellyfinApiClient jellyfinApiClient)
        {
            _jellyfinSecretStore = jellyfinSecretStore;
            _jellyfinApiClient = jellyfinApiClient;
        }

        public Task<Either<BaseError, Unit>> Handle(SaveJellyfinSecrets request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PerformSave)
                .Bind(v => v.ToEitherAsync());

        private async Task<Validation<BaseError, JellyfinSecrets>> Validate(SaveJellyfinSecrets request)
        {
            // TODO: save connection/media source to database
            Either<BaseError, string> maybeServerName = await _jellyfinApiClient.GetServerName(request.Secrets);
            return maybeServerName.Match(
                _ => Validation<BaseError, JellyfinSecrets>.Success(request.Secrets),
                error => error);
        }

        private Task<Unit> PerformSave(JellyfinSecrets jellyfinSecrets) =>
            _jellyfinSecretStore.SaveSecrets(jellyfinSecrets);
    }
}
