using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class ReplaceProgramScheduleItemsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ChannelWriter<IBackgroundServiceRequest> channel) : ProgramScheduleItemCommandBase,
    IRequestHandler<ReplaceProgramScheduleItems, Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>
{
    public async Task<Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>> Handle(
        ReplaceProgramScheduleItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(ps => PersistItems(dbContext, request, ps, cancellationToken));
    }

    private async Task<IEnumerable<ProgramScheduleItemViewModel>> PersistItems(
        TvContext dbContext,
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule,
        CancellationToken cancellationToken)
    {
        dbContext.RemoveRange(programSchedule.Items);

        // reset index starting with zero
        programSchedule.Items = [];
        var orderedItems = request.Items.OrderBy(i => i.Index).ToList();
        for (var i = 0; i < orderedItems.Count; i++)
        {
            programSchedule.Items.Add(BuildItem(programSchedule, i, orderedItems[i]));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // refresh any playouts that use this schedule
        foreach (Playout playout in programSchedule.Playouts)
        {
            await channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh), cancellationToken);
        }

        return programSchedule.Items.Map(ProjectToViewModel);
    }

    private static Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        ReplaceProgramScheduleItems request,
        CancellationToken cancellationToken) =>
        ProgramScheduleMustExist(dbContext, request.ProgramScheduleId, cancellationToken)
            .BindT(programSchedule => PlayoutModesMustBeValid(request, programSchedule))
            .BindT(programSchedule => CollectionTypesMustBeValid(request, programSchedule))
            .BindT(programSchedule => PlaybackOrdersMustBeValid(request, programSchedule))
            .BindT(programSchedule => FillerConfigurationsMustBeValid(dbContext, request, programSchedule));

    private static Validation<BaseError, ProgramSchedule> PlayoutModesMustBeValid(
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule) =>
        request.Items.Map(item => PlayoutModeMustBeValid(item, programSchedule)).Sequence()
            .Map(_ => programSchedule);

    private static Validation<BaseError, ProgramSchedule> CollectionTypesMustBeValid(
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule) =>
        request.Items.Map(item => CollectionTypeMustBeValid(item, programSchedule)).Sequence()
            .Map(_ => programSchedule);

    private static async Task<Validation<BaseError, ProgramSchedule>> FillerConfigurationsMustBeValid(
        TvContext dbContext,
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule)
    {
        foreach (ReplaceProgramScheduleItem item in request.Items)
        {
            Either<BaseError, ProgramSchedule> result = await FillerConfigurationMustBeValid(
                dbContext,
                item,
                programSchedule);
            if (result.IsLeft)
            {
                return result.ToValidation();
            }
        }

        return programSchedule;
    }

    private static Validation<BaseError, ProgramSchedule> PlaybackOrdersMustBeValid(
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule)
    {
        var keyOrders = new Dictionary<CollectionKey, System.Collections.Generic.HashSet<PlaybackOrder>>();
        foreach (ReplaceProgramScheduleItem item in request.Items)
        {
            if (item.PlaybackOrder is PlaybackOrder.ShuffleInOrder &&
                item.FillWithGroupMode is not FillWithGroupMode.None)
            {
                return new BaseError("Shuffle in Order cannot be used with Fill With Group Mode");
            }

            var key = new CollectionKey(
                item.CollectionType,
                item.CollectionId,
                item.MediaItemId,
                item.MultiCollectionId,
                item.SmartCollectionId,
                item.RerunCollectionId,
                item.PlaylistId,
                item.SearchQuery);

            if (keyOrders.TryGetValue(key, out System.Collections.Generic.HashSet<PlaybackOrder> playbackOrders))
            {
                playbackOrders.Add(item.PlaybackOrder);
                keyOrders[key] = playbackOrders;
            }
            else
            {
                keyOrders.Add(key, new System.Collections.Generic.HashSet<PlaybackOrder> { item.PlaybackOrder });
            }
        }

        return Optional(keyOrders.Values.Count(set => set.Count != 1))
            .Filter(count => count == 0)
            .Map(_ => programSchedule)
            .ToValidation<BaseError>("A collection must not use multiple playback orders");
    }

    private sealed record CollectionKey(
        CollectionType CollectionType,
        int? CollectionId,
        int? MediaItemId,
        int? MultiCollectionId,
        int? SmartCollectionId,
        int? RerunCollectionId,
        int? PlaylistId,
        string SearchQuery);
}
