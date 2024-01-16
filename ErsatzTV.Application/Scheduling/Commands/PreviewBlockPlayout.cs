namespace ErsatzTV.Application.Scheduling;

public record PreviewBlockPlayout(ReplaceBlockItems Data) : IRequest<List<PlayoutItemPreviewViewModel>>;
