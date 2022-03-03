using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards;

public record CollectionCardResultsViewModel(
    string Name,
    List<MovieCardViewModel> MovieCards,
    List<TelevisionShowCardViewModel> ShowCards,
    List<TelevisionSeasonCardViewModel> SeasonCards,
    List<TelevisionEpisodeCardViewModel> EpisodeCards,
    List<ArtistCardViewModel> ArtistCards,
    List<MusicVideoCardViewModel> MusicVideoCards,
    List<OtherVideoCardViewModel> OtherVideoCards,
    List<SongCardViewModel> SongCards)
{
    public bool UseCustomPlaybackOrder { get; set; }
}