using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Jellyfin
{
    internal static class Mapper
    {
        internal static JellyfinMediaSourceViewModel ProjectToViewModel(JellyfinMediaSource jellyfinMediaSource) =>
            new(
                jellyfinMediaSource.Id,
                jellyfinMediaSource.ServerName,
                jellyfinMediaSource.Connections.HeadOrNone().Match(c => c.Address, string.Empty));
    }
}
