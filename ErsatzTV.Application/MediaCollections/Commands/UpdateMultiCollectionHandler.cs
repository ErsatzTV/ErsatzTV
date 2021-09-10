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
    public class UpdateMultiCollectionHandler : MediatR.IRequestHandler<UpdateMultiCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateMultiCollectionHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _dbContextFactory = dbContextFactory;
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            UpdateMultiCollection request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, MultiCollection> validation = await Validate(dbContext, request);
            return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
        }

        private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, MultiCollection c, UpdateMultiCollection request)
        {
            c.Name = request.Name;
            
            // save name first so playouts don't get rebuilt for a name change
            await dbContext.SaveChangesAsync();

            var toAdd = request.Items
                .Filter(i => c.MultiCollectionItems.All(i2 => i2.CollectionId != i.CollectionId))
                .Map(
                    i => new MultiCollectionItem
                    {
                        CollectionId = i.CollectionId,
                        MultiCollectionId = c.Id,
                        ScheduleAsGroup = i.ScheduleAsGroup,
                        PlaybackOrder = i.PlaybackOrder
                    })
                .ToList();
            var toRemove = c.MultiCollectionItems
                .Filter(i => request.Items.All(i2 => i2.CollectionId != i.CollectionId))
                .ToList();
            
            // remove items that are no longer present
            c.MultiCollectionItems.RemoveAll(toRemove.Contains);
            
            // update existing items
            foreach (MultiCollectionItem item in c.MultiCollectionItems)
            {
                foreach (UpdateMultiCollectionItem incoming in request.Items.Filter(
                    i => i.CollectionId == item.CollectionId))
                {
                    item.ScheduleAsGroup = incoming.ScheduleAsGroup;
                    item.PlaybackOrder = incoming.PlaybackOrder;
                }
            }

            // add new items
            c.MultiCollectionItems.AddRange(toAdd);

            // rebuild playouts
            if (await dbContext.SaveChangesAsync() > 0)
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingMultiCollection(
                    request.MultiCollectionId))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
        }

        private static async Task<Validation<BaseError, MultiCollection>> Validate(
            TvContext dbContext,
            UpdateMultiCollection request) =>
            (await MultiCollectionMustExist(dbContext, request), await ValidateName(dbContext, request))
            .Apply((collectionToUpdate, _) => collectionToUpdate);

        private static Task<Validation<BaseError, MultiCollection>> MultiCollectionMustExist(
            TvContext dbContext,
            UpdateMultiCollection updateCollection) =>
            dbContext.MultiCollections
                .Include(mc => mc.MultiCollectionItems)
                .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.MultiCollectionId)
                .Map(o => o.ToValidation<BaseError>("MultiCollection does not exist."));

        private static async Task<Validation<BaseError, string>> ValidateName(TvContext dbContext, UpdateMultiCollection updateMultiCollection)
        {
            List<string> allNames = await dbContext.MultiCollections
                .Filter(mc => mc.Id != updateMultiCollection.MultiCollectionId)
                .Map(c => c.Name)
                .ToListAsync();

            Validation<BaseError, string> result1 = updateMultiCollection.NotEmpty(c => c.Name)
                .Bind(_ => updateMultiCollection.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(updateMultiCollection.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("MultiCollection name must be unique");

            return (result1, result2).Apply((_, _) => updateMultiCollection.Name);
        }
    }
}
