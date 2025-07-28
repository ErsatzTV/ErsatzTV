namespace ErsatzTV.Application.Troubleshooting.Queries;

public record GetTroubleshootingSubtitles(int MediaItemId) : IRequest<List<SubtitleViewModel>>;
