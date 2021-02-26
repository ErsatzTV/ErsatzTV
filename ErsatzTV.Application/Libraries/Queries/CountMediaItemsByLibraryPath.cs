using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record CountMediaItemsByLibraryPath(int LibraryPathId) : IRequest<int>;
}
