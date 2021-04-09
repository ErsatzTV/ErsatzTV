using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetPagedCollectionsHandler : IRequestHandler<GetPagedCollections, PagedMediaCollectionsViewModel>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetPagedCollectionsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<PagedMediaCollectionsViewModel> Handle(
            GetPagedCollections request,
            CancellationToken cancellationToken)
        {
            int count = await _mediaCollectionRepository.CountAllCollections();

            List<MediaCollectionViewModel> page = await _mediaCollectionRepository
                .GetPagedCollections(request.PageNum, request.PageSize)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new PagedMediaCollectionsViewModel(count, page);
        }
    }
}
