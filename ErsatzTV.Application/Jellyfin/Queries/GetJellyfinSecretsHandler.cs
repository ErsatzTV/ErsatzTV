using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Jellyfin;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public class GetJellyfinSecretsHandler : IRequestHandler<GetJellyfinSecrets, JellyfinSecrets>
    {
        private readonly IJellyfinSecretStore _jellyfinSecretStore;

        public GetJellyfinSecretsHandler(IJellyfinSecretStore jellyfinSecretStore) =>
            _jellyfinSecretStore = jellyfinSecretStore;

        public Task<JellyfinSecrets> Handle(GetJellyfinSecrets request, CancellationToken cancellationToken) =>
            _jellyfinSecretStore.ReadSecrets();
    }
}
