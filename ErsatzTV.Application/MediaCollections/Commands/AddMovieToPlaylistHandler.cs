using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddMovieToPlaylistHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<AddMovieToPlaylist, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        AddMovieToPlaylist request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(parameters => ApplyAddMovieRequest(dbContext, parameters));
    }

    private static async Task<Unit> ApplyAddMovieRequest(TvContext dbContext, Parameters parameters)
    {
        int index = parameters.Playlist.Items.Count > 0 ? parameters.Playlist.Items.Max(i => i.Index) + 1 : 0;

        var playlistItem = new PlaylistItem
        {
            Index = index,
            CollectionType = CollectionType.Movie,
            MediaItemId = parameters.Movie.Id,
            PlaybackOrder = PlaybackOrder.Shuffle,
            IncludeInProgramGuide = true
        };

        parameters.Playlist.Items.Add(playlistItem);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddMovieToPlaylist request,
        CancellationToken cancellationToken) =>
        (await PlaylistMustExist(dbContext, request, cancellationToken),
            await ValidateMovie(dbContext, request, cancellationToken))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Playlist>> PlaylistMustExist(
        TvContext dbContext,
        AddMovieToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Playlists
            .Include(c => c.Items)
            .SelectOneAsync(c => c.Id, c => c.Id == request.PlaylistId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Playlist does not exist."));

    private static Task<Validation<BaseError, Movie>> ValidateMovie(
        TvContext dbContext,
        AddMovieToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Movies
            .SelectOneAsync(m => m.Id, e => e.Id == request.MovieId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Movie does not exist"));

    private sealed record Parameters(Playlist Playlist, Movie Movie);
}
