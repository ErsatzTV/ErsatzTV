using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class AddOtherVideoToCollectionHandler :
        MediatR.IRequestHandler<AddOtherVideoToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public AddOtherVideoToCollectionHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _dbContextFactory = dbContextFactory;
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            AddOtherVideoToCollection request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
            return await validation.Apply(parameters => ApplyAddOtherVideoRequest(dbContext, parameters));
        }

        private async Task<Unit> ApplyAddOtherVideoRequest(TvContext dbContext, Parameters parameters)
        {
            parameters.Collection.MediaItems.Add(parameters.OtherVideo);
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
            AddOtherVideoToCollection request) =>
            (await CollectionMustExist(dbContext, request), await ValidateOtherVideo(dbContext, request))
            .Apply((collection, episode) => new Parameters(collection, episode));

        private static Task<Validation<BaseError, Collection>> CollectionMustExist(
            TvContext dbContext,
            AddOtherVideoToCollection request) =>
            dbContext.Collections
                .Include(c => c.MediaItems)
                .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
                .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

        private static Task<Validation<BaseError, OtherVideo>> ValidateOtherVideo(
            TvContext dbContext,
            AddOtherVideoToCollection request) =>
            dbContext.OtherVideos
                .SelectOneAsync(m => m.Id, e => e.Id == request.OtherVideoId)
                .Map(o => o.ToValidation<BaseError>("OtherVideo does not exist"));

        private record Parameters(Collection Collection, OtherVideo OtherVideo);
    }
}
