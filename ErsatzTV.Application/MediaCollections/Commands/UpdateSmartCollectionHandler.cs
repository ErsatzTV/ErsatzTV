using System.Collections.Generic;
using System.Linq;
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
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class UpdateSmartCollectionHandler : MediatR.IRequestHandler<UpdateSmartCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateSmartCollectionHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _dbContextFactory = dbContextFactory;
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            UpdateSmartCollection request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, SmartCollection> validation = await Validate(dbContext, request);
            return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
        }

        private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, SmartCollection c, UpdateSmartCollection request)
        {
            c.Query = request.Query;

            // rebuild playouts
            if (await dbContext.SaveChangesAsync() > 0)
            {
                // rebuild all playouts that use this smart collection
                foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingSmartCollection(request.Id))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
        }

        private static Task<Validation<BaseError, SmartCollection>> Validate(
            TvContext dbContext,
            UpdateSmartCollection request) => SmartCollectionMustExist(dbContext, request);

        private static Task<Validation<BaseError, SmartCollection>> SmartCollectionMustExist(
            TvContext dbContext,
            UpdateSmartCollection updateCollection) =>
            dbContext.SmartCollections
                .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.Id)
                .Map(o => o.ToValidation<BaseError>("SmartCollection does not exist."));
    }
}
