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
    public class
        GetMediaCollectionSummariesHandler : IRequestHandler<GetMediaCollectionSummaries,
            List<MediaCollectionSummaryViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetMediaCollectionSummariesHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<List<MediaCollectionSummaryViewModel>> Handle(
            GetMediaCollectionSummaries request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetSummaries(request.SearchString)
                .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
