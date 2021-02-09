using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts.Queries
{
    public class
        GetPlayoutByIdHandler : IRequestHandler<GetPlayoutById, Option<PlayoutViewModel>>
    {
        private readonly IPlayoutRepository _playoutRepository;

        public GetPlayoutByIdHandler(IPlayoutRepository playoutRepository) =>
            _playoutRepository = playoutRepository;

        public Task<Option<PlayoutViewModel>> Handle(
            GetPlayoutById request,
            CancellationToken cancellationToken) =>
            _playoutRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
