using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddItemsToCollectionHandler :
    MediatR.IRequestHandler<AddItemsToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public AddItemsToCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        IMovieRepository movieRepository,
        ITelevisionRepository televisionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _movieRepository = movieRepository;
        _televisionRepository = televisionRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        AddItemsToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Collection> validation = await Validate(dbContext, request);
        return await validation.Apply(c => ApplyAddItemsRequest(dbContext, c, request));
    }

    private async Task<Unit> ApplyAddItemsRequest(TvContext dbContext, Collection collection, AddItemsToCollection request)
    {
        var allItems = request.MovieIds
            .Append(request.ShowIds)
            .Append(request.SeasonIds)
            .Append(request.EpisodeIds)
            .Append(request.ArtistIds)
            .Append(request.MusicVideoIds)
            .Append(request.OtherVideoIds)
            .Append(request.SongIds)
            .ToList();

        var toAddIds = allItems.Where(item => collection.MediaItems.All(mi => mi.Id != item)).ToList();
        List<MediaItem> toAdd = await dbContext.MediaItems
            .Filter(mi => toAddIds.Contains(mi.Id))
            .ToListAsync();

        collection.MediaItems.AddRange(toAdd);

        if (await dbContext.SaveChangesAsync() > 0)
        {
            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository
                .PlayoutIdsUsingCollection(request.CollectionId))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return Unit.Default;
    }

    private async Task<Validation<BaseError, Collection>> Validate(
        TvContext dbContext,
        AddItemsToCollection request) =>
        (await CollectionMustExist(dbContext, request),
            await ValidateMovies(request),
            await ValidateShows(request),
            await ValidateSeasons(request),
            await ValidateEpisodes(request))
        .Apply((collection, _, _, _, _) => collection);

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddItemsToCollection request) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private Task<Validation<BaseError, Unit>> ValidateMovies(AddItemsToCollection request) =>
        _movieRepository.AllMoviesExist(request.MovieIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Movie does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateShows(AddItemsToCollection request) =>
        _televisionRepository.AllShowsExist(request.ShowIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Show does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateSeasons(AddItemsToCollection request) =>
        _televisionRepository.AllSeasonsExist(request.SeasonIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Season does not exist"));

    private Task<Validation<BaseError, Unit>> ValidateEpisodes(AddItemsToCollection request) =>
        _televisionRepository.AllEpisodesExist(request.EpisodeIds)
            .Map(Optional)
            .Filter(v => v == true)
            .MapT(_ => Unit.Default)
            .Map(v => v.ToValidation<BaseError>("Episode does not exist"));
}