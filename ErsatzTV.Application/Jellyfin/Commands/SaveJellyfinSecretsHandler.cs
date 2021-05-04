using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class SaveJellyfinSecretsHandler : MediatR.IRequestHandler<SaveJellyfinSecrets, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IJellyfinBackgroundServiceRequest> _channel;
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly IJellyfinSecretStore _jellyfinSecretStore;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public SaveJellyfinSecretsHandler(
            IJellyfinSecretStore jellyfinSecretStore,
            IJellyfinApiClient jellyfinApiClient,
            IMediaSourceRepository mediaSourceRepository,
            ChannelWriter<IJellyfinBackgroundServiceRequest> channel)
        {
            _jellyfinSecretStore = jellyfinSecretStore;
            _jellyfinApiClient = jellyfinApiClient;
            _mediaSourceRepository = mediaSourceRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(SaveJellyfinSecrets request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PerformSave)
                .Bind(v => v.ToEitherAsync());

        private async Task<Validation<BaseError, Parameters>> Validate(SaveJellyfinSecrets request)
        {
            Either<BaseError, string> maybeServerName = await _jellyfinApiClient.GetServerName(
                request.Secrets.Address,
                request.Secrets.ApiKey);

            return maybeServerName.Match(
                serverName => Validation<BaseError, Parameters>.Success(new Parameters(request.Secrets, serverName)),
                error => error);
        }

        private async Task<Unit> PerformSave(Parameters parameters)
        {
            await _jellyfinSecretStore.SaveSecrets(parameters.Secrets);
            await _mediaSourceRepository.UpsertJellyfin(parameters.Secrets.Address, parameters.ServerName);
            await _channel.WriteAsync(new SynchronizeJellyfinMediaSources());

            return Unit.Default;
        }

        private record Parameters(JellyfinSecrets Secrets, string ServerName);
    }
}
