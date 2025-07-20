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
    List<SongCardViewModel> SongCards,
    List<ImageCardViewModel> ImageCards,
    List<RemoteStreamCardViewModel> RemoteStreamCards)
{
    public bool UseCustomPlaybackOrder { get; set; }
}
