using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateSmartCollectionHandler : IRequestHandler<UpdateSmartCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly ISearchTargets _searchTargets;
    private readonly ISmartCollectionCache _smartCollectionCache;

    public UpdateSmartCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel,
        ISearchTargets searchTargets,
        ISmartCollectionCache smartCollectionCache)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
        _searchTargets = searchTargets;
        _smartCollectionCache = smartCollectionCache;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateSmartCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, SmartCollection> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, SmartCollection c, UpdateSmartCollection request)
    {
        c.Query = request.Query;
        c.Name = request.Name;

        // rebuild playouts
        if (await dbContext.SaveChangesAsync() > 0)
        {
            _searchTargets.SearchTargetsChanged();
            await _smartCollectionCache.Refresh();

            // refresh all playouts that use this smart collection
            foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingSmartCollection(request.Id))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, SmartCollection>> Validate(
        TvContext dbContext,
        UpdateSmartCollection request,
        CancellationToken cancellationToken) => SmartCollectionMustExist(dbContext, request, cancellationToken);

    private static Task<Validation<BaseError, SmartCollection>> SmartCollectionMustExist(
        TvContext dbContext,
        UpdateSmartCollection updateCollection,
        CancellationToken cancellationToken) =>
        dbContext.SmartCollections
            .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.Id, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("SmartCollection does not exist."));
}
