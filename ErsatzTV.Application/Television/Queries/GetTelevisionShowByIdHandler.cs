﻿using System.Collections.Generic;
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
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionShowByIdHandler(
            ITelevisionRepository televisionRepository,
            ISearchRepository searchRepository,
            IMediaSourceRepository mediaSourceRepository)
        {
            _televisionRepository = televisionRepository;
            _searchRepository = searchRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Option<TelevisionShowViewModel>> Handle(
            GetTelevisionShowById request,
            CancellationToken cancellationToken)
        {
            Option<Show> maybeShow = await _televisionRepository.GetShow(request.Id);
            return await maybeShow.Match<Task<Option<TelevisionShowViewModel>>>(
                async show =>
                {
                    Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                        .Map(list => list.HeadOrNone());
                    
                    List<string> languages = await _searchRepository.GetLanguagesForShow(show);
                    return ProjectToViewModel(show, languages, maybeJellyfin);
                },
                () => Task.FromResult(Option<TelevisionShowViewModel>.None));
        }
    }
}
