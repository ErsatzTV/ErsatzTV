using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries.Queries
{
    public class GetAllLibrariesHandler : IRequestHandler<GetAllLibraries, List<LibraryViewModel>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetAllLibrariesHandler(ILibraryRepository libraryRepository) => _libraryRepository = libraryRepository;

        public Task<List<LibraryViewModel>> Handle(GetAllLibraries request, CancellationToken cancellationToken) =>
            _libraryRepository.GetAll()
                .Map(list => list.Filter(ShouldIncludeLibrary).Map(ProjectToViewModel).ToList());

        private static bool ShouldIncludeLibrary(Library library) =>
            library switch
            {
                LocalLibrary => true,
                PlexLibrary plex => plex.ShouldSyncItems,
                _ => false
            };
    }
}
