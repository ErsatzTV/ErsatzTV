using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Movies;

public record MovieViewModel(
    string Title,
    string Year,
    string Plot,
    List<string> Genres,
    List<string> Tags,
    List<string> Studios,
    List<string> ContentRatings,
    List<string> Languages,
    List<ActorCardViewModel> Actors,
    List<string> Directors,
    List<string> Writers,
    string Path,
    string LocalPath,
    MediaItemState MediaItemState)
{
    public string Poster { get; set; }
    public string FanArt { get; set; }
}
