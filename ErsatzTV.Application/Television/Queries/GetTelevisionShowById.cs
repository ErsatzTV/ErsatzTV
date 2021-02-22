using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Television.Queries
{
    public record GetTelevisionShowById(int Id) : IRequest<Option<TelevisionShowViewModel>>;
}
