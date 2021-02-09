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
    public class GetPlayoutItemsByIdHandler : IRequestHandler<GetPlayoutItemsById, List<PlayoutItemViewModel>>
    {
        private readonly IPlayoutRepository _playoutRepository;

        public GetPlayoutItemsByIdHandler(IPlayoutRepository playoutRepository) =>
            _playoutRepository = playoutRepository;

        public Task<List<PlayoutItemViewModel>> Handle(
            GetPlayoutItemsById request,
            CancellationToken cancellationToken) =>
            _playoutRepository.GetPlayoutItems(request.PlayoutId)
                .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
