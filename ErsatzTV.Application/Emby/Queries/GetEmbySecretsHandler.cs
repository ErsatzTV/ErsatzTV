using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public class GetEmbySecretsHandler : IRequestHandler<GetEmbySecrets, EmbySecrets>
    {
        private readonly IEmbySecretStore _embySecretStore;

        public GetEmbySecretsHandler(IEmbySecretStore embySecretStore) =>
            _embySecretStore = embySecretStore;

        public Task<EmbySecrets> Handle(GetEmbySecrets request, CancellationToken cancellationToken) =>
            _embySecretStore.ReadSecrets();
    }
}
