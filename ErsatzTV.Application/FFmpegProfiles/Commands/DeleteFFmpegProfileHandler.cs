using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class DeleteFFmpegProfileHandler : IRequestHandler<DeleteFFmpegProfile, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, FFmpegProfile> validation = await FFmpegProfileMustExist(dbContext, request);
        return await LanguageExtensions.Apply(validation, p => DoDeletion(dbContext, p));
    }

    private static async Task<Unit> DoDeletion(TvContext dbContext, FFmpegProfile ffmpegProfile)
    {
        dbContext.FFmpegProfiles.Remove(ffmpegProfile);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        DeleteFFmpegProfile request) =>
        dbContext.FFmpegProfiles
            .SelectOneAsync(p => p.Id, p => p.Id == request.FFmpegProfileId)
            .Map(o => o.ToValidation<BaseError>($"FFmpegProfile {request.FFmpegProfileId} does not exist"));
}
