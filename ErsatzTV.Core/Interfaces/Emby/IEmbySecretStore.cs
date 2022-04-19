using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.MediaSources;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbySecretStore : IRemoteMediaSourceSecretStore<EmbySecrets>
{
}
