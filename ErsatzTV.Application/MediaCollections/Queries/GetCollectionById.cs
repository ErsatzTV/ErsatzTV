using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetCollectionById(int Id) : IRequest<Option<MediaCollectionViewModel>>;
}
