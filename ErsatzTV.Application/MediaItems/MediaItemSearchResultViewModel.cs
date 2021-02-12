namespace ErsatzTV.Application.MediaItems
{
    public record MediaItemSearchResultViewModel(
        int Id,
        string Source,
        string MediaType,
        string Title,
        string Duration);
}
