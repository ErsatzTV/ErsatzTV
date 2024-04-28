using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddItemsToPlaylistHandler : IRequestHandler<AddItemsToPlaylist, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMovieRepository _movieRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public AddItemsToPlaylistHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMovieRepository movieRepository,
        ITelevisionRepository televisionRepository)
    {
        _dbContextFactory = dbContextFactory;
        _movieRepository = movieRepository;
        _televisionRepository = televisionRepository;
    }

    public async Task<Either<BaseError, Unit>> Handle(AddItemsToPlaylist request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playlist> validation = await Validate(dbContext, request);
        return await validation.Apply(c => ApplyAddItemsRequest(dbContext, c, request));
    }

    private static async Task<Unit> ApplyAddItemsRequest(
        TvContext dbContext,
        Playlist playlist,
        AddItemsToPlaylist request)
    {
        var allItems = new Dictionary<ProgramScheduleItemCollectionType, List<int>>
        {
            { ProgramScheduleItemCollectionType.Movie, request.MovieIds },
            { ProgramScheduleItemCollectionType.TelevisionShow, request.ShowIds },
            { ProgramScheduleItemCollectionType.TelevisionSeason, request.SeasonIds },
            { ProgramScheduleItemCollectionType.Episode, request.EpisodeIds },
            { ProgramScheduleItemCollectionType.Artist, request.ArtistIds },
            { ProgramScheduleItemCollectionType.MusicVideo, request.MusicVideoIds },
            { ProgramScheduleItemCollectionType.OtherVideo, request.OtherVideoIds },
            { ProgramScheduleItemCollectionType.Song, request.SongIds },
            { ProgramScheduleItemCollectionType.Image, request.ImageIds }
        };

        int index = playlist.Items.Max(i => i.Index) + 1;

        foreach ((ProgramScheduleItemCollectionType collectionType, List<int> ids) in allItems)
        {
            foreach (int id in ids)
            {
                var item = new PlaylistItem
                {
                    Index = index++,
                    CollectionType = collectionType,
                    MediaItemId = id,
                    PlaybackOrder = PlaybackOrder.Shuffle,
                    IncludeInProgramGuide = true
                };

                playlist.Items.Add(item);
            }
        }

        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }

    private async Task<Validation<BaseError, Playlist>> Validate(
        TvContext dbContext,
        AddItemsToPlaylist request) =>
        (await PlaylistMustExist(dbContext, request),
            await ValidateMovies(request),
            await ValidateShows(request),
            await ValidateSeasons(request),
            await ValidateEpisodes(request))
        .Apply((collection, _, _, _, _) => collection);

    private static Task<Validation<BaseError, Playlist>> PlaylistMustExist(
        TvContext dbContext,
        AddItemsToPlaylist request) =>
        dbContext.Playlists
            .Include(c => c.Items)
            .SelectOneAsync(c => c.Id, c => c.Id == request.PlaylistId)
            .Map(o => o.ToValidation<BaseError>("Playlist does not exist."));

    private Task<Validation<BaseError, Unit>> ValidateMovies(AddItemsToPlaylist request) =>
        _movieRepository.AllMoviesExist(request.MovieIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Movie does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateShows(AddItemsToPlaylist request) =>
        _televisionRepository.AllShowsExist(request.ShowIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Show does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateSeasons(AddItemsToPlaylist request) =>
        _televisionRepository.AllSeasonsExist(request.SeasonIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Season does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateEpisodes(AddItemsToPlaylist request) =>
        _televisionRepository.AllEpisodesExist(request.EpisodeIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Episode does not exist"));
}
