using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Configuration;

public record SaveConfigElementByKey(ConfigElementKey Key, string Value) : IRequest;
