using System.Globalization;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Television;

public record TelevisionShowViewModel(
    int Id,
    int LibraryId,
    MediaSourceKind MediaSourceKind,
    string Title,
    string Year,
    string Plot,
    string Poster,
    string FanArt,
    List<string> Genres,
    List<string> Tags,
    List<string> Studios,
    List<string> Networks,
    List<string> ContentRatings,
    List<CultureInfo> Languages,
    List<ActorCardViewModel> Actors);
