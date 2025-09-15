using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddSeasonToPlaylistHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<AddSeasonToPlaylist, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        AddSeasonToPlaylist request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(parameters => ApplyAddSeasonRequest(dbContext, parameters));
    }

    private static async Task<Unit> ApplyAddSeasonRequest(TvContext dbContext, Parameters parameters)
    {
        int index = parameters.Playlist.Items.Count > 0 ? parameters.Playlist.Items.Max(i => i.Index) + 1 : 0;

        var playlistItem = new PlaylistItem
        {
            Index = index,
            CollectionType = CollectionType.TelevisionSeason,
            MediaItemId = parameters.Season.Id,
            PlaybackOrder = PlaybackOrder.Shuffle,
            IncludeInProgramGuide = true
        };

        parameters.Playlist.Items.Add(playlistItem);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddSeasonToPlaylist request,
        CancellationToken cancellationToken) =>
        (await PlaylistMustExist(dbContext, request, cancellationToken),
            await ValidateSeason(dbContext, request, cancellationToken))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Playlist>> PlaylistMustExist(
        TvContext dbContext,
        AddSeasonToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Playlists
            .Include(c => c.Items)
            .SelectOneAsync(c => c.Id, c => c.Id == request.PlaylistId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Playlist does not exist."));

    private static Task<Validation<BaseError, Season>> ValidateSeason(
        TvContext dbContext,
        AddSeasonToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Seasons
            .SelectOneAsync(m => m.Id, e => e.Id == request.SeasonId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Season does not exist"));

    private sealed record Parameters(Playlist Playlist, Season Season);
}
