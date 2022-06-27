using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Serilog.Events;

namespace ErsatzTV.Application.Configuration;

public class GetGeneralSettingsHandler : IRequestHandler<GetGeneralSettings, GeneralSettingsViewModel>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetGeneralSettingsHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<GeneralSettingsViewModel> Handle(GetGeneralSettings request, CancellationToken cancellationToken)
    {
        Option<LogEventLevel> maybeLogLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevel);

        return new GeneralSettingsViewModel
        {
            MinimumLogLevel = await maybeLogLevel.IfNoneAsync(LogEventLevel.Information)
        };
    }
}
