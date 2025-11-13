using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Artists.Mapper;

namespace ErsatzTV.Application.Artists;

public class GetArtistByIdHandler(
    IArtistRepository artistRepository,
    ISearchRepository searchRepository,
    ILanguageCodeService languageCodeService)
    : IRequestHandler<GetArtistById, Option<ArtistViewModel>>
{
    public async Task<Option<ArtistViewModel>> Handle(
        GetArtistById request,
        CancellationToken cancellationToken)
    {
        Option<Artist> maybeArtist = await artistRepository.GetArtist(request.ArtistId);
        return await maybeArtist.Match<Task<Option<ArtistViewModel>>>(
            async artist =>
            {
                List<string> mediaCodes = await searchRepository.GetLanguagesForArtist(artist);
                List<string> languageCodes = languageCodeService.GetAllLanguageCodes(mediaCodes);
                return ProjectToViewModel(artist, languageCodes);
            },
            () => Task.FromResult(Option<ArtistViewModel>.None));
    }
}
