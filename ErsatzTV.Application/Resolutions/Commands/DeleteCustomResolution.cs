using ErsatzTV.Core;

namespace ErsatzTV.Application.Resolutions;

public record DeleteCustomResolution(int ResolutionId) : IRequest<Option<BaseError>>;
