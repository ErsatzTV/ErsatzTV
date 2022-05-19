namespace ErsatzTV.Application.Libraries;

public record CountMediaItemsByLibrary(int LibraryId) : IRequest<int>;
