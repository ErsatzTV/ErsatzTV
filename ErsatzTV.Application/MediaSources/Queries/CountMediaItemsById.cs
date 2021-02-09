using MediatR;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public record CountMediaItemsById(int MediaSourceId) : IRequest<int>;
}
