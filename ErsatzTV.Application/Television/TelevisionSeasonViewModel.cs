namespace ErsatzTV.Application.Television
{
    public record TelevisionSeasonViewModel(
        int Id,
        int ShowId,
        string Title,
        string Year,
        string Name,
        string Poster,
        string FanArt);
}
