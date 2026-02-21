using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetSeekTextSubtitleProcessHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IFFmpegProcessService ffmpegProcessService)
    : IRequestHandler<GetSeekTextSubtitleProcess,
        Either<BaseError, SeekTextSubtitleProcess>>
{
    public async Task<Either<BaseError, SeekTextSubtitleProcess>> Handle(
        GetSeekTextSubtitleProcess request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, string> validation = await Validate(dbContext, cancellationToken);
        return await validation.Match(
            ffmpegPath => GetProcess(request, ffmpegPath, cancellationToken),
            error => Task.FromResult<Either<BaseError, SeekTextSubtitleProcess>>(error.Join()));
    }

    private async Task<Either<BaseError, SeekTextSubtitleProcess>> GetProcess(
        GetSeekTextSubtitleProcess request,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        Command process = await ffmpegProcessService.SeekTextSubtitle(
            ffmpegPath,
            request.PathAndCodec.Path,
            request.PathAndCodec.Codec,
            request.Seek,
            cancellationToken);

        return new SeekTextSubtitleProcess(process);
    }

    private static async Task<Validation<BaseError, string>> Validate(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        await FFmpegPathMustExist(dbContext, cancellationToken);

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath, cancellationToken)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));
}
