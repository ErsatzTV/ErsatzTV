using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class GetXmltvSettingsHandler(IConfigElementRepository configElementRepository)
    : IRequestHandler<GetXmltvSettings, XmltvSettingsViewModel>
{
    public async Task<XmltvSettingsViewModel> Handle(GetXmltvSettings request, CancellationToken cancellationToken)
    {
        Option<int> daysToBuild = await configElementRepository.GetValue<int>(ConfigElementKey.XmltvDaysToBuild);

        Option<XmltvTimeZone> maybeTimeZone =
            await configElementRepository.GetValue<XmltvTimeZone>(ConfigElementKey.XmltvTimeZone);

        return new XmltvSettingsViewModel
        {
            DaysToBuild = await daysToBuild.IfNoneAsync(2),
            TimeZone = await maybeTimeZone.IfNoneAsync(XmltvTimeZone.Local)
        };
    }
}
