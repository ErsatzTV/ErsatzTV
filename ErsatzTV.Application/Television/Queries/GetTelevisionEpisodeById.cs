using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Television.Queries
{
    public record GetTelevisionEpisodeById(int EpisodeId) : IRequest<Option<TelevisionEpisodeViewModel>>;
}
