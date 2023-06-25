using ErsatzTV.Core;

namespace ErsatzTV.Application.Resolutions;

public record CreateCustomResolution(int Width, int Height) : IRequest<Option<BaseError>>;
