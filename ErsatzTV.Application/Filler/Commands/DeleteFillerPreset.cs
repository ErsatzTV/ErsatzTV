using ErsatzTV.Core;

namespace ErsatzTV.Application.Filler;

public record DeleteFillerPreset(int FillerPresetId) : IRequest<Either<BaseError, Unit>>;
