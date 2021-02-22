using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class
        GetTelevisionEpisodeByIdHandler : IRequestHandler<GetTelevisionEpisodeById, Option<TelevisionEpisodeViewModel>>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionEpisodeByIdHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public Task<Option<TelevisionEpisodeViewModel>> Handle(
            GetTelevisionEpisodeById request,
            CancellationToken cancellationToken) =>
            _televisionRepository.GetEpisode(request.EpisodeId)
                .MapT(ProjectToViewModel);
    }
}
