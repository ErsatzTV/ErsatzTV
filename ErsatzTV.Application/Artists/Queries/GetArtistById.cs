using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Artists.Queries
{
    public record GetArtistById(int ArtistId) : IRequest<Option<ArtistViewModel>>;
}
