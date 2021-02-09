using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetMediaItemById(int Id) : IRequest<Option<MediaItemViewModel>>;
}
