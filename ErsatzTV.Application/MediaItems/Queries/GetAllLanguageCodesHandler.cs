using System.Globalization;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.MediaItems;

public class GetAllLanguageCodesHandler : IRequestHandler<GetAllLanguageCodes, List<CultureInfo>>
{
    private readonly IMediaItemRepository _mediaItemRepository;

    public GetAllLanguageCodesHandler(IMediaItemRepository mediaItemRepository) =>
        _mediaItemRepository = mediaItemRepository;

    public async Task<List<CultureInfo>> Handle(GetAllLanguageCodes request, CancellationToken cancellationToken) =>
        await _mediaItemRepository.GetAllLanguageCodeCultures();
}
