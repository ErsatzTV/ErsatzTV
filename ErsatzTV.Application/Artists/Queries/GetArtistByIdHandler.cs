﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Artists.Mapper;

namespace ErsatzTV.Application.Artists;

public class GetArtistByIdHandler : IRequestHandler<GetArtistById, Option<ArtistViewModel>>
{
    private readonly IArtistRepository _artistRepository;
    private readonly ISearchRepository _searchRepository;

    public GetArtistByIdHandler(IArtistRepository artistRepository, ISearchRepository searchRepository)
    {
        _artistRepository = artistRepository;
        _searchRepository = searchRepository;
    }

    public async Task<Option<ArtistViewModel>> Handle(
        GetArtistById request,
        CancellationToken cancellationToken)
    {
        Option<Artist> maybeArtist = await _artistRepository.GetArtist(request.ArtistId);
        return await maybeArtist.Match<Task<Option<ArtistViewModel>>>(
            async artist =>
            {
                List<string> mediaCodes = await _searchRepository.GetLanguagesForArtist(artist);
                List<string> languageCodes = await _searchRepository.GetAllThreeLetterLanguageCodes(mediaCodes);
                return ProjectToViewModel(artist, languageCodes);
            },
            () => Task.FromResult(Option<ArtistViewModel>.None));
    }
}
