using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public record RefreshSearchIndex : MediatR.IRequest<Unit>, IBackgroundServiceRequest;
}
