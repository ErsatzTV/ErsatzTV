namespace ErsatzTV.Application.Television
{
    public record TelevisionEpisodeViewModel(
        int ShowId,
        int SeasonId,
        int Episode,
        string Title,
        string Plot,
        string Poster);
}
