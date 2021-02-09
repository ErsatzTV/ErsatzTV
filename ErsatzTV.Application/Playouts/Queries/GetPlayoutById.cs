using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts.Queries
{
    public record GetPlayoutById(int Id) : IRequest<Option<PlayoutViewModel>>;
}
