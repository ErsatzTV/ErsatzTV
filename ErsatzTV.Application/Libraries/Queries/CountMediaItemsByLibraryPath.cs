namespace ErsatzTV.Application.Libraries;

public record CountMediaItemsByLibraryPath(int LibraryPathId) : IRequest<int>;
