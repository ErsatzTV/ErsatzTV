using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class
    CopyProgramScheduleHandler : IRequestHandler<CopyProgramSchedule, Either<BaseError, ProgramScheduleViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CopyProgramScheduleHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, ProgramScheduleViewModel>> Handle(
        CopyProgramSchedule request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request);
            return await validation.Apply(p => PerformCopy(dbContext, p, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private async Task<ProgramScheduleViewModel> PerformCopy(
        TvContext dbContext,
        ProgramSchedule schedule,
        CopyProgramSchedule request,
        CancellationToken cancellationToken)
    {
        var clone = new ProgramSchedule();
        await dbContext.AddAsync(clone, cancellationToken);

        clone.Name = request.Name;
        clone.RandomStartPoint = schedule.RandomStartPoint;
        clone.ShuffleScheduleItems = schedule.ShuffleScheduleItems;
        clone.TreatCollectionsAsShows = schedule.TreatCollectionsAsShows;
        clone.KeepMultiPartEpisodesTogether = schedule.KeepMultiPartEpisodesTogether;

        // no playouts, no alternates
        clone.Playouts = new List<Playout>();
        clone.ProgramScheduleAlternates = new List<ProgramScheduleAlternate>();

        // clone all items
        clone.Items = new List<ProgramScheduleItem>();
        foreach (ProgramScheduleItem item in schedule.Items)
        {
            PropertyValues itemValues = dbContext.Entry(item).CurrentValues.Clone();
            itemValues["Id"] = 0;

            ProgramScheduleItem itemClone = item switch
            {
                ProgramScheduleItemFlood => new ProgramScheduleItemFlood(),
                ProgramScheduleItemDuration => new ProgramScheduleItemDuration(),
                ProgramScheduleItemMultiple => new ProgramScheduleItemMultiple(),
                _ => new ProgramScheduleItemOne()
            };

            await dbContext.AddAsync(itemClone, cancellationToken);
            dbContext.Entry(itemClone).CurrentValues.SetValues(itemValues);

            itemClone.ProgramScheduleId = 0;
            itemClone.ProgramSchedule = clone;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ProjectToViewModel(clone);
    }

    private static async Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        CopyProgramSchedule request) =>
        (await ScheduleMustExist(dbContext, request), await ValidateName(dbContext, request))
        .Apply((programSchedule, _) => programSchedule);

    private static Task<Validation<BaseError, ProgramSchedule>> ScheduleMustExist(
        TvContext dbContext,
        CopyProgramSchedule request) =>
        dbContext.ProgramSchedules
            .AsNoTracking()
            .Include(ps => ps.Items)
            .SelectOneAsync(p => p.Id, p => p.Id == request.ProgramScheduleId)
            .Map(o => o.ToValidation<BaseError>("Schedule does not exist."));

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CopyProgramSchedule request)
    {
        List<string> allNames = await dbContext.ProgramSchedules
            .Map(ps => ps.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = request.NotEmpty(c => c.Name)
            .Bind(_ => request.NotLongerThan(50)(c => c.Name));

        var result2 = Optional(request.Name)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("Schedule name must be unique");

        return (result1, result2).Apply((_, _) => request.Name);
    }
}
