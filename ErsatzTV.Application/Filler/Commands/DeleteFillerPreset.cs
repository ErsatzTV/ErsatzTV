using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Filler;

public record DeleteFillerPreset(int FillerPresetId) : IRequest<Either<BaseError, LanguageExt.Unit>>;