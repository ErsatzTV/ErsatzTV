using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Application.Configuration.Commands
{
    public record SaveConfigElementByKey(ConfigElementKey Key, string Value) : MediatR.IRequest<Unit>;
}
