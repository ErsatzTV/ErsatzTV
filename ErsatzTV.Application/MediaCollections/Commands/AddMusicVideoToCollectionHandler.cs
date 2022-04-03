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

public class AddMusicVideoToCollectionHandler :
    MediatR.IRequestHandler<AddMusicVideoToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public AddMusicVideoToCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        AddMusicVideoToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await validation.Apply(parameters => ApplyAddMusicVideoRequest(dbContext, parameters));
    }

    private async Task<Unit> ApplyAddMusicVideoRequest(TvContext dbContext, Parameters parameters)
    {
        parameters.Collection.MediaItems.Add(parameters.MusicVideo);
        if (await dbContext.SaveChangesAsync() > 0)
        {
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
        AddMusicVideoToCollection request) =>
        (await CollectionMustExist(dbContext, request), await ValidateMusicVideo(dbContext, request))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddMusicVideoToCollection request) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Task<Validation<BaseError, MusicVideo>> ValidateMusicVideo(
        TvContext dbContext,
        AddMusicVideoToCollection request) =>
        dbContext.MusicVideos
            .SelectOneAsync(m => m.Id, e => e.Id == request.MusicVideoId)
            .Map(o => o.ToValidation<BaseError>("MusicVideo does not exist"));

    private record Parameters(Collection Collection, MusicVideo MusicVideo);
}