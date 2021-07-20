using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record CountMediaItemsByLibrary(int LibraryId) : IRequest<int>;
}
