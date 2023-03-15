using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Subtitles.Queries;

public class GetSubtitlePathByIdHandler : IRequestHandler<GetSubtitlePathById, Either<BaseError, string>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetSubtitlePathByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, string>> Handle(
        GetSubtitlePathById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Option<string> maybeSubtitlePath = await dbContext.Subtitles
            .SelectOneAsync(s => s.Id, s => s.Id == request.Id)
            .MapT(s => s.Path);
        return maybeSubtitlePath.ToEither(BaseError.New($"Unable to locate subtitle with id {request.Id}"));
    }
}
