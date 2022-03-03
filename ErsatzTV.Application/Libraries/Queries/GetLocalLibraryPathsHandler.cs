using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries;

public class GetLocalLibraryPathsHandler : IRequestHandler<GetLocalLibraryPaths, List<LocalLibraryPathViewModel>>
{
    private readonly ILibraryRepository _libraryRepository;

    public GetLocalLibraryPathsHandler(ILibraryRepository libraryRepository) =>
        _libraryRepository = libraryRepository;

    public Task<List<LocalLibraryPathViewModel>> Handle(
        GetLocalLibraryPaths request,
        CancellationToken cancellationToken) =>
        _libraryRepository.GetLocalPaths(request.LocalLibraryId)
            .Map(list => list.Map(ProjectToViewModel).ToList());
}