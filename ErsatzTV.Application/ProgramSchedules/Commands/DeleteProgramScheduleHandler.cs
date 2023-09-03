using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public class DeleteProgramScheduleHandler : IRequestHandler<DeleteProgramSchedule, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteProgramScheduleHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteProgramSchedule request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ProgramSchedule> validation = await ProgramScheduleMustExist(dbContext, request);
        return await validation.Apply(ps => DoDeletion(dbContext, ps));
    }

    private static Task<Unit> DoDeletion(TvContext dbContext, ProgramSchedule programSchedule)
    {
        dbContext.ProgramSchedules.Remove(programSchedule);
        return dbContext.SaveChangesAsync().ToUnit();
    }

    private static Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
        TvContext dbContext,
        DeleteProgramSchedule request) =>
        dbContext.ProgramSchedules
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.ProgramScheduleId)
            .Map(o => o.ToValidation<BaseError>($"ProgramSchedule {request.ProgramScheduleId} does not exist."));
}
