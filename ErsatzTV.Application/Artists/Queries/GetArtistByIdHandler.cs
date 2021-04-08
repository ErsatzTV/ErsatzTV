using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Artists.Mapper;

namespace ErsatzTV.Application.Artists.Queries
{
    public class GetArtistByIdHandler : IRequestHandler<GetArtistById, Option<ArtistViewModel>>
    {
        private readonly IArtistRepository _artistRepository;

        public GetArtistByIdHandler(IArtistRepository artistRepository) => _artistRepository = artistRepository;

        public Task<Option<ArtistViewModel>> Handle(
            GetArtistById request,
            CancellationToken cancellationToken) =>
            _artistRepository.GetArtist(request.ArtistId).MapT(ProjectToViewModel);
    }
}
