using ErsatzTV.Core.Interfaces.MediaSources;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinSecretStore : IRemoteMediaSourceSecretStore<JellyfinSecrets>
{
}
