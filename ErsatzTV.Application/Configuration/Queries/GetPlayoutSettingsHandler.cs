using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class GetPlayoutSettingsHandler : IRequestHandler<GetPlayoutSettings, PlayoutSettingsViewModel>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetPlayoutSettingsHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<PlayoutSettingsViewModel> Handle(GetPlayoutSettings request, CancellationToken cancellationToken)
    {
        Option<int> daysToBuild = await _configElementRepository.GetValue<int>(
            ConfigElementKey.PlayoutDaysToBuild,
            cancellationToken);

        Option<bool> skipMissingItems =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.PlayoutSkipMissingItems, cancellationToken);

        Option<int> scriptedScheduleTimeoutSeconds =
            await _configElementRepository.GetValue<int>(
                ConfigElementKey.PlayoutScriptedScheduleTimeoutSeconds,
                cancellationToken);

        return new PlayoutSettingsViewModel
        {
            DaysToBuild = await daysToBuild.IfNoneAsync(2),
            SkipMissingItems = await skipMissingItems.IfNoneAsync(false),
            ScriptedScheduleTimeoutSeconds = await scriptedScheduleTimeoutSeconds.IfNoneAsync(30)
        };
    }
}
