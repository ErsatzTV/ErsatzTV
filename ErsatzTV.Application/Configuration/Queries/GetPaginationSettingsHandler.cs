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

        Option<int> maybePageSize =
            maybeElement.Bind(element =>
                int.TryParse(element.Value, out int value)
                    ? Prelude.Some(value)
                    : Option<int>.None);

        int defaultPageSize = PaginationOptions.NormalizePageSize(maybePageSize.ToNullable());

        return new PaginationSettingsViewModel { DefaultPageSize = defaultPageSize };
    }
}
