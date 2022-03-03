using ErsatzTV.Core.Domain;
using Flurl;
using LanguageExt;

namespace ErsatzTV.Core.Jellyfin;

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

        return Url.Parse(address)
            .AppendPathSegment(pathSegment)
            .SetQueryParams(query);
    }

    public static Url ForArtwork(string address, string artwork)
    {
        string[] split = artwork.Replace("jellyfin://", string.Empty).Split('?');
        if (split.Length != 2)
        {
            return artwork;
        }

        string pathSegment = split[0];
        QueryParamCollection query = Url.ParseQueryParams(split[1]);

        return Url.Parse(address)
            .AppendPathSegment(pathSegment)
            .SetQueryParams(query);
    }

    public static Url ProxyForArtwork(string scheme, string host, string artwork, ArtworkKind artworkKind)
    {
        string[] split = artwork.Replace("jellyfin://", string.Empty).Split('?');
        if (split.Length != 2)
        {
            return artwork;
        }

        string pathSegment = split[0];
        QueryParamCollection query = Url.ParseQueryParams(split[1]);

        string artworkFolder = artworkKind switch
        {
            ArtworkKind.Thumbnail => "thumbnails",
            _ => "posters"
        };

        return Url.Parse($"{scheme}://{host}/iptv/artwork/{artworkFolder}/jellyfin")
            .AppendPathSegment(pathSegment)
            .SetQueryParams(query);
    }
        
    public static Url RelativeProxyForArtwork(string artwork)
    {
        string[] split = artwork.Replace("jellyfin://", string.Empty).Split('?');
        if (split.Length != 2)
        {
            return artwork;
        }

        string pathSegment = split[0];
        QueryParamCollection query = Url.ParseQueryParams(split[1]);

        return Url.Parse("jellyfin")
            .AppendPathSegment(pathSegment)
            .SetQueryParams(query);
    }

}