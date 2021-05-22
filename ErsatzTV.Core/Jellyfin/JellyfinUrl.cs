using ErsatzTV.Core.Domain;
using Flurl;
using LanguageExt;

namespace ErsatzTV.Core.Jellyfin
{
    public static class JellyfinUrl
    {
        public static Url ForArtwork(Option<JellyfinMediaSource> maybeJellyfin, string artwork)
        {
            string address = maybeJellyfin.Map(ms => ms.Connections.HeadOrNone().Map(c => c.Address))
                .Flatten()
                .IfNone("jellyfin://");

            string[] split = artwork.Replace("jellyfin://", string.Empty).Split('?');
            if (split.Length != 2)
            {
                return artwork;
            }

            string pathSegment = split[0];
            QueryParamCollection query = Url.ParseQueryParams(split[1]);

            Url x = Url.Parse(address)
                .AppendPathSegment(pathSegment)
                .SetQueryParams(query);

            return x;
        }
    }
}
