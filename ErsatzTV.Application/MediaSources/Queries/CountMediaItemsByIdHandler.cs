using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public class CountMediaItemsByIdHandler : IRequestHandler<CountMediaItemsById, int>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public CountMediaItemsByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<int> Handle(CountMediaItemsById request, CancellationToken cancellationToken) =>
            _mediaSourceRepository.CountMediaItems(request.MediaSourceId);
    }
}
