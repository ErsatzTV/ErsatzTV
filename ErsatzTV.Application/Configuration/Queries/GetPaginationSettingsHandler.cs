using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Configuration;

public class GetPaginationSettingsHandler : IRequestHandler<GetPaginationSettings, PaginationSettingsViewModel>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetPaginationSettingsHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<PaginationSettingsViewModel> Handle(
        GetPaginationSettings request,
        CancellationToken cancellationToken)
    {
        Option<ConfigElement> maybeElement = await _configElementRepository.GetConfigElement(
            ConfigElementKey.PagesDefaultPageSize,
            cancellationToken);

        int defaultPageSize = PaginationOptions.NormalizePageSize(
            maybeElement.Bind<int?>(element => int.TryParse(element.Value, out int value) ? value : null));

        return new PaginationSettingsViewModel { DefaultPageSize = defaultPageSize };
    }
}
