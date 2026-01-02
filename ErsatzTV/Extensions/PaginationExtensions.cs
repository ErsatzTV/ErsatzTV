using ErsatzTV.Application.Configuration;
using ErsatzTV.Core;
using MediatR;

namespace ErsatzTV.Extensions;

public static class PaginationExtensions
{
    public static async Task<int> GetDefaultPageSize(this IMediator mediator, CancellationToken cancellationToken)
    {
        PaginationSettingsViewModel settings = await mediator.Send(new GetPaginationSettings(), cancellationToken);
        return PaginationOptions.NormalizePageSize(settings.DefaultPageSize);
    }
}
