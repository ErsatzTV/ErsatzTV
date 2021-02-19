using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class GetTelevisionShowByIdHandler : IRequestHandler<GetTelevisionShowById, Option<TelevisionShowViewModel>>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionShowByIdHandler(ITelevisionRepository televisionRepository)
        {
            _televisionRepository = televisionRepository;
        }

        public Task<Option<TelevisionShowViewModel>> Handle(
            GetTelevisionShowById request,
            CancellationToken cancellationToken) =>
            _televisionRepository.GetShow(request.Id)
                .MapT(ProjectToViewModel);
    }
}
