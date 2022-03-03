using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Libraries;

public class CountMediaItemsByLibraryPathHandler : IRequestHandler<CountMediaItemsByLibraryPath, int>
{
    private readonly ILibraryRepository _libraryRepository;

    public CountMediaItemsByLibraryPathHandler(ILibraryRepository libraryRepository) =>
        _libraryRepository = libraryRepository;

    public Task<int> Handle(CountMediaItemsByLibraryPath request, CancellationToken cancellationToken) =>
        _libraryRepository.CountMediaItemsByPath(request.LibraryPathId);
}