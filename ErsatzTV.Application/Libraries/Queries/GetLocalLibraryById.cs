using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record GetLocalLibraryById(int LibraryId) : IRequest<Option<LocalLibraryViewModel>>;
}
