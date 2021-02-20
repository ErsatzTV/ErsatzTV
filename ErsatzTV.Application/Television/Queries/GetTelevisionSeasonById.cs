using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Television.Queries
{
    public record GetTelevisionSeasonById(int SeasonId) : IRequest<Option<TelevisionSeasonViewModel>>;
}
