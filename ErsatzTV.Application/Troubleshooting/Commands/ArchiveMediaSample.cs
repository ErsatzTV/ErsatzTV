namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveMediaSample(int MediaItemId) : IRequest<Option<string>>;
