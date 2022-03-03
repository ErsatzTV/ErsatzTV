using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Artists;

public record GetArtistById(int ArtistId) : IRequest<Option<ArtistViewModel>>;