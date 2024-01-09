using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class UpdateExternalJsonPlayoutHandler : IRequestHandler<UpdateExternalJsonPlayout, Either<BaseError, PlayoutNameViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateExternalJsonPlayoutHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, PlayoutNameViewModel>> Handle(
        UpdateExternalJsonPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
    }

    private static async Task<PlayoutNameViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        UpdateExternalJsonPlayout request,
        Playout playout)
    {
        playout.ExternalJsonFile = request.ExternalJsonFile;
        
        await dbContext.SaveChangesAsync();

        return new PlayoutNameViewModel(
            playout.Id,
            playout.ProgramSchedulePlayoutType,
            playout.Channel.Name,
            playout.Channel.Number,
            playout.ProgramSchedule?.Name ?? string.Empty,
            playout.ExternalJsonFile,
            Optional(playout.DailyRebuildTime));
    }

    private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, UpdateExternalJsonPlayout request) =>
        PlayoutMustExist(dbContext, request);

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        UpdateExternalJsonPlayout updatePlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == updatePlayout.PlayoutId)
            .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
}
