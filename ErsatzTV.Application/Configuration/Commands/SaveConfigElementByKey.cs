using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Application.Configuration;

public record SaveConfigElementByKey(ConfigElementKey Key, string Value) : MediatR.IRequest<Unit>;