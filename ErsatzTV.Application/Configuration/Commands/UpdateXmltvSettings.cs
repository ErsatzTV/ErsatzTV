using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateXmltvSettings(XmltvSettingsViewModel XmltvSettings) : IRequest<Either<BaseError, Unit>>;
