using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries.Queries
{
    public class GetLocalLibrariesHandler : IRequestHandler<GetLocalLibraries, List<LocalLibraryViewModel>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLocalLibrariesHandler(ILibraryRepository libraryRepository) => _libraryRepository = libraryRepository;

        public Task<List<LocalLibraryViewModel>>
            Handle(GetLocalLibraries request, CancellationToken cancellationToken) =>
            _libraryRepository.GetAllLocal().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
