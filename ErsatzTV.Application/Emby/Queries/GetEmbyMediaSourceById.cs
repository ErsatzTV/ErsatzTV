using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public record GetEmbyMediaSourceById(int EmbyMediaSourceId) : IRequest<Option<EmbyMediaSourceViewModel>>;
}
