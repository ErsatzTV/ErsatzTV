using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetMultiCollectionById(int Id) : IRequest<Option<MultiCollectionViewModel>>;
}
