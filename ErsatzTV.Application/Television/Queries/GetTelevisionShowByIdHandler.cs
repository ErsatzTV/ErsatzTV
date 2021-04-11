using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class GetTelevisionShowByIdHandler : IRequestHandler<GetTelevisionShowById, Option<TelevisionShowViewModel>>
    {
        private readonly ISearchRepository _searchRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionShowByIdHandler(
            ITelevisionRepository televisionRepository,
            ISearchRepository searchRepository)
        {
            _televisionRepository = televisionRepository;
            _searchRepository = searchRepository;
        }

        public async Task<Option<TelevisionShowViewModel>> Handle(
            GetTelevisionShowById request,
            CancellationToken cancellationToken)
        {
            Option<Show> maybeShow = await _televisionRepository.GetShow(request.Id);
            return await maybeShow.Match<Task<Option<TelevisionShowViewModel>>>(
                async show =>
                {
                    List<string> languages = await _searchRepository.GetLanguagesForShow(show);
                    return ProjectToViewModel(show, languages);
                },
                () => Task.FromResult(Option<TelevisionShowViewModel>.None));
        }
    }
}
