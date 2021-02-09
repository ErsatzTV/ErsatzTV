using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaSources.Mapper;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public class GetMediaSourceByIdHandler : IRequestHandler<GetMediaSourceById, Option<MediaSourceViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetMediaSourceByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Option<MediaSourceViewModel>> Handle(
            GetMediaSourceById request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
