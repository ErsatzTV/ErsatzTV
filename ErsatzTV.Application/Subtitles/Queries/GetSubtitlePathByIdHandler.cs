using Dapper;
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
        
        // TODO: check for plex path
        Option<int> maybePlexId = await dbContext.Connection.QuerySingleOrDefaultAsync<int?>(
                @"select PMS.Id from PlexMediaSource PMS
                     inner join Library L on PMS.Id = L.MediaSourceId
                     inner join LibraryPath LP on L.Id = LP.LibraryId
                     inner join MediaItem MI on LP.Id = MI.LibraryPathId
                     inner join EpisodeMetadata EM on EM.EpisodeId = MI.Id
                     inner join Subtitle S on EM.Id = S.EpisodeMetadataId
                     where S.Id = @SubtitleId",
                new { SubtitleId = request.Id })
            .Map(Optional);

        if (maybePlexId.IsNone)
        {
            maybePlexId = await dbContext.Connection.QuerySingleOrDefaultAsync<int?>(
                    @"select PMS.Id from PlexMediaSource PMS
                     inner join Library L on PMS.Id = L.MediaSourceId
                     inner join LibraryPath LP on L.Id = LP.LibraryId
                     inner join MediaItem MI on LP.Id = MI.LibraryPathId
                     inner join MovieMetadata MM on MM.MovieId = MI.Id
                     inner join Subtitle S on MM.Id = S.MovieMetadataId
                     where S.Id = @SubtitleId",
                    new { SubtitleId = request.Id })
                .Map(Optional);
        }

        foreach (int plexMediaSourceId in maybePlexId)
        {
            foreach (string subtitlePath in maybeSubtitlePath)
            {
                return $"http://localhost:{Settings.ListenPort}/media/plex/{plexMediaSourceId}/{subtitlePath}";
            }
        }
        
        // TODO: check for jellyfin path
        // TODO: check for emby path
        
        return maybeSubtitlePath.ToEither(BaseError.New($"Unable to locate subtitle with id {request.Id}"));
    }
}
