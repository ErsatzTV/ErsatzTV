using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Configuration;

public record GetConfigElementByKey(ConfigElementKey Key) : IRequest<Option<ConfigElementViewModel>>;
