namespace ErsatzTV.Application.Search;

public record QuerySearchIndexAllItems(string Query) : IRequest<SearchResultAllItemsViewModel>;
