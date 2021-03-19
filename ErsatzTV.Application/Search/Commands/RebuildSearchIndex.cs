using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public record RebuildSearchIndex : MediatR.IRequest<Unit>, IBackgroundServiceRequest;
}
