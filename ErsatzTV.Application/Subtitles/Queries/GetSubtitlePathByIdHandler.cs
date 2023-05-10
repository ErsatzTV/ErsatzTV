using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Subtitles.Queries;

public class GetSubtitlePathByIdHandler : IRequestHandler<GetSubtitlePathById, Either<BaseError, string>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetSubtitlePathByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, string>> Handle(
        GetSubtitlePathById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Option<Subtitle> maybeSubtitle = await dbContext.Subtitles
            .SelectOneAsync(s => s.Id, s => s.Id == request.Id);

        foreach (string plexUrl in await GetPlexUrl(request, dbContext, maybeSubtitle))
        {
            return plexUrl;
        }

        foreach (string jellyfinUrl in await GetJellyfinUrl(request, dbContext, maybeSubtitle))
        {
            return jellyfinUrl;
        }

        foreach (string embyUrl in await GetEmbyUrl(request, dbContext, maybeSubtitle))
        {
            return embyUrl;
        }

        return maybeSubtitle
            .Map(s => s.Path)
            .ToEither(BaseError.New($"Unable to locate subtitle with id {request.Id}"));
    }

    private static async Task<Option<string>> GetPlexUrl(
        GetSubtitlePathById request,
        TvContext dbContext,
        Option<Subtitle> maybeSubtitle)
    {
        // check for plex episode
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

        // check for plex movie
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
            foreach (string subtitlePath in maybeSubtitle.Map(s => s.Path))
            {
                return $"http://localhost:{Settings.ListenPort}/media/plex/{plexMediaSourceId}/{subtitlePath}";
            }
        }

        return Option<string>.None;
    }

    private static async Task<Option<string>> GetJellyfinUrl(
        GetSubtitlePathById request,
        TvContext dbContext,
        Option<Subtitle> maybeSubtitle)
    {
        // check for jellyfin episode
        Option<string> maybeJellyfinId = await dbContext.Connection.QuerySingleOrDefaultAsync<string>(
                @"select JE.ItemId from JellyfinEpisode JE
                     inner join EpisodeMetadata EM on EM.EpisodeId = JE.Id
                     inner join Subtitle S on EM.Id = S.EpisodeMetadataId
                     where S.Id = @SubtitleId",
                new { SubtitleId = request.Id })
            .Map(Optional);

        // check for jellyfin movie
        if (maybeJellyfinId.IsNone)
        {
            maybeJellyfinId = await dbContext.Connection.QuerySingleOrDefaultAsync<string>(
                    @"select JM.ItemId from JellyfinMovie JM
                     inner join MovieMetadata MM on MM.MovieId = JM.Id
                     inner join Subtitle S on MM.Id = S.MovieMetadataId
                     where S.Id = @SubtitleId",
                    new { SubtitleId = request.Id })
                .Map(Optional);
        }

        foreach (string jellyfinItemId in maybeJellyfinId)
        {
            foreach (Subtitle subtitle in maybeSubtitle)
            {
                int index = subtitle.StreamIndex - JellyfinStream.ExternalStreamOffset;
                string extension = Subtitle.ExtensionForCodec(subtitle.Codec);
                var subtitlePath =
                    $"Videos/{jellyfinItemId}/{jellyfinItemId}/Subtitles/{index}/{index}/Stream.{extension}";
                return $"http://localhost:{Settings.ListenPort}/media/jellyfin/{subtitlePath}";
            }
        }

        return Option<string>.None;
    }

    private static async Task<Option<string>> GetEmbyUrl(
        GetSubtitlePathById request,
        TvContext dbContext,
        Option<Subtitle> maybeSubtitle)
    {
        // check for emby episode
        Option<string> maybeEmbyId = await dbContext.Connection.QuerySingleOrDefaultAsync<string>(
                @"select EE.ItemId from EmbyEpisode EE
                     inner join EpisodeMetadata EM on EM.EpisodeId = EE.Id
                     inner join Subtitle S on EM.Id = S.EpisodeMetadataId
                     where S.Id = @SubtitleId",
                new { SubtitleId = request.Id })
            .Map(Optional);

        // check for emby movie
        if (maybeEmbyId.IsNone)
        {
            maybeEmbyId = await dbContext.Connection.QuerySingleOrDefaultAsync<string>(
                    @"select EM.ItemId from EmbyMovie EM
                     inner join MovieMetadata MM on MM.MovieId = EM.Id
                     inner join Subtitle S on MM.Id = S.MovieMetadataId
                     where S.Id = @SubtitleId",
                    new { SubtitleId = request.Id })
                .Map(Optional);
        }

        foreach (string embyItemId in maybeEmbyId)
        {
            foreach (Subtitle subtitle in maybeSubtitle)
            {
                string extension = Subtitle.ExtensionForCodec(subtitle.Codec);
                var subtitlePath =
                    $"Videos/{embyItemId}/{subtitle.Path}/Subtitles/{subtitle.StreamIndex}/Stream.{extension}";
                return $"http://localhost:{Settings.ListenPort}/media/emby/{subtitlePath}";
            }
        }

        return Option<string>.None;
    }
}
