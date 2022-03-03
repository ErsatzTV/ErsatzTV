using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class ReplaceProgramScheduleItemsHandler : ProgramScheduleItemCommandBase,
    IRequestHandler<ReplaceProgramScheduleItems, Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;

    public ReplaceProgramScheduleItemsHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _channel = channel;
    }

    public async Task<Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>> Handle(
        ReplaceProgramScheduleItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, ps => PersistItems(dbContext, request, ps));
    }

    private async Task<IEnumerable<ProgramScheduleItemViewModel>> PersistItems(
        TvContext dbContext,
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule)
    {
        dbContext.RemoveRange(programSchedule.Items);
        programSchedule.Items = request.Items.Map(i => BuildItem(programSchedule, i.Index, i)).ToList();

        await dbContext.SaveChangesAsync();

        // rebuild any playouts that use this schedule
        foreach (Playout playout in programSchedule.Playouts)
        {
            await _channel.WriteAsync(new BuildPlayout(playout.Id, true));
        }

        return programSchedule.Items.Map(ProjectToViewModel);
    }

    private Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        ReplaceProgramScheduleItems request) =>
        ProgramScheduleMustExist(dbContext, request.ProgramScheduleId)
            .BindT(programSchedule => PlayoutModesMustBeValid(request, programSchedule))
            .BindT(programSchedule => CollectionTypesMustBeValid(request, programSchedule))
            .BindT(programSchedule => PlaybackOrdersMustBeValid(request, programSchedule))
            .BindT(programSchedule => FillerConfigurationsMustBeValid(dbContext, request, programSchedule));

    private static Validation<BaseError, ProgramSchedule> PlayoutModesMustBeValid(
        ReplaceProgramScheduleItems request,
        ProgramSchedule programSchedule) =>
        request.Items.Map(item => PlayoutModeMustBeValid(item, programSchedule)).Sequence()
            .Map(_ => programSchedule);

    private Validation<BaseError, ProgramSchedule> CollectionTypesMustBeValid(
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
            var key = new CollectionKey(
                item.CollectionType,
                item.CollectionId,
                item.MediaItemId,
                item.MultiCollectionId,
                item.SmartCollectionId);

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

    private record CollectionKey(
        ProgramScheduleItemCollectionType CollectionType,
        int? CollectionId,
        int? MediaItemId,
        int? MultiCollectionId,
        int? SmartCollectionId);
}