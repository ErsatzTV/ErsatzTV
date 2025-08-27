using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Artworks;

public class GetArtworkHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetArtwork, Either<BaseError, Artwork>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Artwork>> Handle(
        GetArtwork request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<Artwork> artwork = await dbContext.Artwork
                .AsNoTracking()
                .SelectOneAsync(a => a.Id, a => a.Id == request.Id, cancellationToken)
                .MapT(Project);

            return artwork.ToEither(BaseError.New("Artwork not found"));
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private static Artwork Project(Artwork artwork) =>
        new()
        {
            Id = artwork.Id,
            Path = artwork.Path,
            ArtworkKind = artwork.ArtworkKind
        };
}
