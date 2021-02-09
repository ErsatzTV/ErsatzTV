using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts.Queries
{
    public class GetAllPlayoutsHandler : IRequestHandler<GetAllPlayouts, List<PlayoutViewModel>>
    {
        private readonly IPlayoutRepository _playoutRepository;

        public GetAllPlayoutsHandler(IPlayoutRepository playoutRepository) =>
            _playoutRepository = playoutRepository;

        public Task<List<PlayoutViewModel>> Handle(
            GetAllPlayouts request,
            CancellationToken cancellationToken) =>
            _playoutRepository.GetAll().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
