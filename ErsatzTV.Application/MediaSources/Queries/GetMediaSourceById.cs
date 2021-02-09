using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public record GetMediaSourceById(int Id) : IRequest<Option<MediaSourceViewModel>>;
}
