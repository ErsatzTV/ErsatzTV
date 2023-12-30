using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
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

    private static async Task<ProgramScheduleViewModel> PerformCopy(
        TvContext dbContext,
        ProgramSchedule schedule,
        CopyProgramSchedule request,
        CancellationToken cancellationToken)
    {
        DetachEntity(dbContext, schedule);
        schedule.Name = request.Name;

        // no playouts, no alternates
        schedule.Playouts = new List<Playout>();
        schedule.ProgramScheduleAlternates = new List<ProgramScheduleAlternate>();

        foreach (ProgramScheduleItem item in schedule.Items)
        {
            DetachEntity(dbContext, item);
            item.ProgramScheduleId = 0;
            item.ProgramSchedule = schedule;
        }

        await dbContext.ProgramSchedules.AddAsync(schedule, cancellationToken);
        await dbContext.ProgramScheduleItems.AddRangeAsync(schedule.Items, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ProjectToViewModel(schedule);
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

    private static void DetachEntity<T>(TvContext db, T entity) where T : class
    {
        db.Entry(entity).State = EntityState.Detached;
        if (entity.GetType().GetProperty("Id") is not null)
        {
            entity.GetType().GetProperty("Id")!.SetValue(entity, 0);
        }
    }
}
