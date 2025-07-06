using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Application.MediaItems;

public class GetAllLanguageCodesHandler(IMediaItemRepository mediaItemRepository)
    : IRequestHandler<GetAllLanguageCodes, List<LanguageCodeViewModel>>
{
    public async Task<List<LanguageCodeViewModel>> Handle(
        GetAllLanguageCodes request,
        CancellationToken cancellationToken)
    {
        List<LanguageCodeAndName> languageCodes = await mediaItemRepository.GetAllLanguageCodesAndNames();
        return languageCodes.Map(c => new LanguageCodeViewModel(c.Code, c.Name)).ToList();
    }
}
