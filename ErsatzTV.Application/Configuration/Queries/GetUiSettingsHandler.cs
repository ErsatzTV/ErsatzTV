using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class GetUiSettingsHandler(IConfigElementRepository configElementRepository)
    : IRequestHandler<GetUiSettings, UiSettingsViewModel>
{
    public async Task<UiSettingsViewModel> Handle(GetUiSettings request, CancellationToken cancellationToken)
    {
        Option<bool> pagesIsDarkMode = await configElementRepository.GetValue<bool>(
            ConfigElementKey.PagesIsDarkMode,
            cancellationToken);

        return new UiSettingsViewModel
        {
            PagesIsDarkMode = await pagesIsDarkMode.IfNoneAsync(true)
        };
    }
}
