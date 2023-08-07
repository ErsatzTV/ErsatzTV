using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Resolutions;

public class DeleteCustomResolutionHandler : IRequestHandler<DeleteCustomResolution, Option<BaseError>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteCustomResolutionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<BaseError>> Handle(DeleteCustomResolution request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Resolution> maybeResolution = await dbContext.Resolutions
            .AsNoTracking()
            .SelectOneAsync(p => p.Id, p => p.Id == request.ResolutionId && p.IsCustom == true);

        foreach (Resolution resolution in maybeResolution)
        {
            // reset any ffmpeg profiles using this resolution to 1920x1080
            await dbContext.Connection.ExecuteAsync(
                @"UPDATE FFmpegProfile SET ResolutionId = 3 WHERE ResolutionId = @ResolutionId",
                new { request.ResolutionId });

            dbContext.Resolutions.Remove(resolution);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeResolution.IsNone
            ? BaseError.New($"Resolution {request.ResolutionId} does not exist.")
            : Option<BaseError>.None;
    }
}
