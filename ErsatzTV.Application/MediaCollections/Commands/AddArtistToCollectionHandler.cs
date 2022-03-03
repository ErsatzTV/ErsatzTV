using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddArtistToCollectionHandler :
    MediatR.IRequestHandler<AddArtistToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public AddArtistToCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        AddArtistToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, parameters => ApplyAddArtistRequest(dbContext, parameters));
    }

    private async Task<Unit> ApplyAddArtistRequest(TvContext dbContext, Parameters parameters)
    {
        parameters.Collection.MediaItems.Add(parameters.Artist);
        if (await dbContext.SaveChangesAsync() > 0)
        {
            // rebuild all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository
                         .PlayoutIdsUsingCollection(parameters.Collection.Id))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, true));
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddArtistToCollection request) =>
        (await CollectionMustExist(dbContext, request), await ValidateArtist(dbContext, request))
        .Apply((collection, artist) => new Parameters(collection, artist));

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddArtistToCollection request) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Task<Validation<BaseError, Artist>> ValidateArtist(
        TvContext dbContext,
        AddArtistToCollection request) =>
        dbContext.Artists
            .SelectOneAsync(a => a.Id, a => a.Id == request.ArtistId)
            .Map(o => o.ToValidation<BaseError>("Artist does not exist"));

    private record Parameters(Collection Collection, Artist Artist);
}