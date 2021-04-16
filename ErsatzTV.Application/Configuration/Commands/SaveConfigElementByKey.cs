using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Application.Configuration.Commands
{
    public record SaveConfigElementByKey(ConfigElementKey Key, string Value) : IRequest<LanguageExt.Unit>;
}
