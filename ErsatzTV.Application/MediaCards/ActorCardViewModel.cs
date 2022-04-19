using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record ActorCardViewModel(int Id, string Name, string Role, string Thumb, MediaItemState State) :
    MediaCardViewModel(Id, Name, Role, Name, Thumb, State);
