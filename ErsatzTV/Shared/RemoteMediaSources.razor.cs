using ErsatzTV.Application.MediaSources;
using ErsatzTV.Core.MediaSources;

namespace ErsatzTV.Shared;

public partial class RemoteMediaSources<TViewModel, TSecrets, TMediaSource>
    where TViewModel : RemoteMediaSourceViewModel
    where TSecrets : RemoteMediaSourceSecrets
{
}