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

public class AddImageToCollectionHandler : IRequestHandler<AddImageToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _searchChannel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public AddImageToCollectionHandler(
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
        AddImageToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await validation.Apply(parameters => ApplyAddImageRequest(dbContext, parameters));
    }

    private async Task<Unit> ApplyAddImageRequest(TvContext dbContext, Parameters parameters)
    {
        parameters.Collection.MediaItems.Add(parameters.Image);
        if (await dbContext.SaveChangesAsync() > 0)
        {
            await _searchChannel.WriteAsync(new ReindexMediaItems([parameters.Image.Id]));

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
        AddImageToCollection request) =>
        (await CollectionMustExist(dbContext, request), await ValidateImage(dbContext, request))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddImageToCollection request) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Task<Validation<BaseError, Image>> ValidateImage(
        TvContext dbContext,
        AddImageToCollection request) =>
        dbContext.Images
            .SelectOneAsync(m => m.Id, e => e.Id == request.ImageId)
            .Map(o => o.ToValidation<BaseError>("Image does not exist"));

    private sealed record Parameters(Collection Collection, Image Image);
}
