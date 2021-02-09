using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetSimpleMediaCollectionById(int Id) : IRequest<Option<MediaCollectionViewModel>>;
}
