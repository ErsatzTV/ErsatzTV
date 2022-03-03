using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Application.Jellyfin;

public class GetJellyfinSecretsHandler : IRequestHandler<GetJellyfinSecrets, JellyfinSecrets>
{
    private readonly IJellyfinSecretStore _jellyfinSecretStore;

    public GetJellyfinSecretsHandler(IJellyfinSecretStore jellyfinSecretStore) =>
        _jellyfinSecretStore = jellyfinSecretStore;

    public Task<JellyfinSecrets> Handle(GetJellyfinSecrets request, CancellationToken cancellationToken) =>
        _jellyfinSecretStore.ReadSecrets();
}