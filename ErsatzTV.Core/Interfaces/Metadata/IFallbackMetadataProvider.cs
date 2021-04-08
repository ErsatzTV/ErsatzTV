using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IFallbackMetadataProvider
    {
        ShowMetadata GetFallbackMetadataForShow(string showFolder);
        ArtistMetadata GetFallbackMetadataForArtist(string artistFolder);
        Tuple<EpisodeMetadata, int> GetFallbackMetadata(Episode episode);
        MovieMetadata GetFallbackMetadata(Movie movie);
        MusicVideoMetadata GetFallbackMetadata(MusicVideo musicVideo);
        string GetSortTitle(string title);
    }
}
