using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries.Queries
{
    public class GetLocalLibraryByIdHandler : IRequestHandler<GetLocalLibraryById, Option<LocalLibraryViewModel>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLocalLibraryByIdHandler(ILibraryRepository libraryRepository) =>
            _libraryRepository = libraryRepository;

        public Task<Option<LocalLibraryViewModel>> Handle(
            GetLocalLibraryById request,
            CancellationToken cancellationToken) =>
            _libraryRepository.GetLocal(request.LibraryId).MapT(ProjectToViewModel);
    }
}
