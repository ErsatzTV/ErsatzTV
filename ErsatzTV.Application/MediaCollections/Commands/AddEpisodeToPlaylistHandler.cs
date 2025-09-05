using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddEpisodeToPlaylistHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<AddEpisodeToPlaylist, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        AddEpisodeToPlaylist request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(parameters => ApplyAddEpisodeRequest(dbContext, parameters));
    }

    private static async Task<Unit> ApplyAddEpisodeRequest(TvContext dbContext, Parameters parameters)
    {
        int index = parameters.Playlist.Items.Count > 0 ? parameters.Playlist.Items.Max(i => i.Index) + 1 : 0;

        var playlistItem = new PlaylistItem
        {
            Index = index,
            CollectionType = ProgramScheduleItemCollectionType.Episode,
            MediaItemId = parameters.Episode.Id,
            PlaybackOrder = PlaybackOrder.Shuffle,
            IncludeInProgramGuide = true
        };

        parameters.Playlist.Items.Add(playlistItem);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddEpisodeToPlaylist request,
        CancellationToken cancellationToken) =>
        (await PlaylistMustExist(dbContext, request, cancellationToken),
            await ValidateEpisode(dbContext, request, cancellationToken))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Playlist>> PlaylistMustExist(
        TvContext dbContext,
        AddEpisodeToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Playlists
            .Include(c => c.Items)
            .SelectOneAsync(c => c.Id, c => c.Id == request.PlaylistId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Playlist does not exist."));

    private static Task<Validation<BaseError, Episode>> ValidateEpisode(
        TvContext dbContext,
        AddEpisodeToPlaylist request,
        CancellationToken cancellationToken) =>
        dbContext.Episodes
            .SelectOneAsync(m => m.Id, e => e.Id == request.EpisodeId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Episode does not exist"));

    private sealed record Parameters(Playlist Playlist, Episode Episode);
}
