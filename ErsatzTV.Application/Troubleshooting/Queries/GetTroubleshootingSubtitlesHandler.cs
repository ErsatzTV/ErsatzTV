using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Troubleshooting.Queries;

public class GetTroubleshootingSubtitlesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTroubleshootingSubtitles, List<SubtitleViewModel>>
{
    public async Task<List<SubtitleViewModel>> Handle(
        GetTroubleshootingSubtitles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Option<MediaItem> maybeMediaItem = await dbContext.MediaItems
            .AsNoTracking()
            .Include(mi => (mi as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(mi => (mi as Episode).EpisodeMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(mi => (mi as OtherVideo).OtherVideoMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .SelectOneAsync(mi => mi.Id, mi => mi.Id == request.MediaItemId, cancellationToken);

        foreach (MediaItem mediaItem in maybeMediaItem)
        {
            List<Subtitle> subtitles = GetSubtitles(mediaItem);

            // remove text subtitles that are embedded but have not been extracted
            subtitles.RemoveAll(s => s.SubtitleKind is SubtitleKind.Embedded && !s.IsImage && !s.IsExtracted);

            return subtitles.Map(ProjectToViewModel).ToList();
        }

        return [];
    }

    private static List<Subtitle> GetSubtitles(MediaItem mediaItem) =>
        mediaItem switch
        {
            Episode e => e.EpisodeMetadata.Head().Subtitles,
            Movie m => m.MovieMetadata.Head().Subtitles,
            OtherVideo ov => ov.OtherVideoMetadata.Head().Subtitles,
            _ => []
        };

    private static SubtitleViewModel ProjectToViewModel(Subtitle subtitle) =>
        new(subtitle.Id, subtitle.Language, subtitle.Title, subtitle.Codec);
}
