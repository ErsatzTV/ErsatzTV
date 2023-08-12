using System.Globalization;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.MediaItems;

public class GetAllLanguageCodesHandler : IRequestHandler<GetAllLanguageCodes, List<LanguageCodeViewModel>>
{
    private readonly IMediaItemRepository _mediaItemRepository;

    public GetAllLanguageCodesHandler(IMediaItemRepository mediaItemRepository) =>
        _mediaItemRepository = mediaItemRepository;

    public async Task<List<LanguageCodeViewModel>> Handle(GetAllLanguageCodes request, CancellationToken cancellationToken)
    {
        List<CultureInfo> cultures = await _mediaItemRepository.GetAllLanguageCodeCultures();
        return cultures.Map(c => new LanguageCodeViewModel(c.ThreeLetterISOLanguageName, c.EnglishName)).ToList();
    }
}
