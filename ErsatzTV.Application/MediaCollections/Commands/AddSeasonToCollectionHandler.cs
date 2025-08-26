using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddSeasonToCollectionHandler :
    IRequestHandler<AddSeasonToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _searchChannel;

    public AddSeasonToCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> searchChannel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
        _searchChannel = searchChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        AddSeasonToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(parameters => ApplyAddSeasonRequest(dbContext, parameters));
    }

    private async Task<Unit> ApplyAddSeasonRequest(TvContext dbContext, Parameters parameters)
    {
        parameters.Collection.MediaItems.Add(parameters.Season);
        if (await dbContext.SaveChangesAsync() > 0)
        {
            await _searchChannel.WriteAsync(new ReindexMediaItems([parameters.Season.Id]));

            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository
                         .PlayoutIdsUsingCollection(parameters.Collection.Id))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddSeasonToCollection request,
        CancellationToken cancellationToken) =>
        (await CollectionMustExist(dbContext, request, cancellationToken),
            await ValidateSeason(dbContext, request, cancellationToken))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddSeasonToCollection request,
        CancellationToken cancellationToken) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Task<Validation<BaseError, Season>> ValidateSeason(
        TvContext dbContext,
        AddSeasonToCollection request,
        CancellationToken cancellationToken) =>
        dbContext.Seasons
            .SelectOneAsync(m => m.Id, e => e.Id == request.SeasonId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Season does not exist"));

    private sealed record Parameters(Collection Collection, Season Season);
}
