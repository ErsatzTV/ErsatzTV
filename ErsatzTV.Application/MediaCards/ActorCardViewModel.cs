namespace ErsatzTV.Application.MediaCards
{
    public record ActorCardViewModel(int Id, string Name, string Role, string Thumb) :
        MediaCardViewModel(Id, Name, Role, Name, Thumb);
}
