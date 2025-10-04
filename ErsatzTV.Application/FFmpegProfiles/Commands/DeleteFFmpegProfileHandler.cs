using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class DeleteFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    : IRequestHandler<DeleteFFmpegProfile, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        DeleteFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FFmpegProfile> validation =
            await FFmpegProfileMustNotBeUsed(dbContext, request, cancellationToken)
                .BindT(_ => FFmpegProfileMustExist(dbContext, request, cancellationToken));
        return await validation.Apply(p => DoDeletion(dbContext, p));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, FFmpegProfile ffmpegProfile)
    {
        dbContext.FFmpegProfiles.Remove(ffmpegProfile);
        await dbContext.SaveChangesAsync();
        searchTargets.SearchTargetsChanged();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        DeleteFFmpegProfile request,
        CancellationToken cancellationToken) =>
        dbContext.FFmpegProfiles
            .SelectOneAsync(p => p.Id, p => p.Id == request.FFmpegProfileId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>($"FFmpegProfile {request.FFmpegProfileId} does not exist"));

    private static async Task<Validation<BaseError, Unit>> FFmpegProfileMustNotBeUsed(
        TvContext dbContext,
        DeleteFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        int count = await dbContext.Channels
            .AsNoTracking()
            .Where(c => c.FFmpegProfileId == request.FFmpegProfileId)
            .CountAsync(cancellationToken);

        if (count > 0)
        {
            return BaseError.New($"Cannot delete FFmpegProfile {request.FFmpegProfileId} that is used by {count} {(count > 1 ? "channels" : "channel")}");
        }

        return Unit.Default;
    }
}
