namespace ErsatzTV.Application.Resolutions;

public record GetResolutionByName(string Name) : IRequest<Option<ResolutionViewModel>>;
