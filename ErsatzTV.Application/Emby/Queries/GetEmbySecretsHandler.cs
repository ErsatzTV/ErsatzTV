using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;

namespace ErsatzTV.Application.Emby;

public class GetEmbySecretsHandler : IRequestHandler<GetEmbySecrets, EmbySecrets>
{
    private readonly IEmbySecretStore _embySecretStore;

    public GetEmbySecretsHandler(IEmbySecretStore embySecretStore) =>
        _embySecretStore = embySecretStore;

    public Task<EmbySecrets> Handle(GetEmbySecrets request, CancellationToken cancellationToken) =>
        _embySecretStore.ReadSecrets();
}