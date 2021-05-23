using ErsatzTV.Core.Domain;
using Flurl;
using LanguageExt;

namespace ErsatzTV.Core.Emby
{
    public static class EmbyUrl
    {
        public static Url ForArtwork(Option<EmbyMediaSource> maybeEmby, string artwork)
        {
            string address = maybeEmby.Map(ms => ms.Connections.HeadOrNone().Map(c => c.Address))
                .Flatten()
                .IfNone("emby://");

            string[] split = artwork.Replace("emby://", string.Empty).Split('?');
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
