using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries;

public class GetAllLocalLibrariesHandler : IRequestHandler<GetAllLocalLibraries, List<LocalLibraryViewModel>>
{
    private readonly ILibraryRepository _libraryRepository;

    public GetAllLocalLibrariesHandler(ILibraryRepository libraryRepository) => _libraryRepository = libraryRepository;

    public Task<List<LocalLibraryViewModel>> Handle(
        GetAllLocalLibraries request,
        CancellationToken cancellationToken) =>
        _libraryRepository.GetAll()
            .Map(
                list => list
                    .OfType<LocalLibrary>()
                    .OrderBy(l => l.MediaKind)
                    .Map(ProjectToViewModel)
                    .ToList());
}