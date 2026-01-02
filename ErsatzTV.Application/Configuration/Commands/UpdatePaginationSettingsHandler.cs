using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using System.Globalization;
using LanguageExt;

namespace ErsatzTV.Application.Configuration;

public class UpdatePaginationSettingsHandler : IRequestHandler<UpdatePaginationSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;

    public UpdatePaginationSettingsHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<Either<BaseError, Unit>> Handle(
        UpdatePaginationSettings request,
        CancellationToken cancellationToken)
    {
        int pageSize = PaginationOptions.NormalizePageSize(request.Settings.DefaultPageSize);

        await _configElementRepository.Upsert(
            ConfigElementKey.PagesDefaultPageSize,
            pageSize.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

        return Unit.Default;
    }
}
