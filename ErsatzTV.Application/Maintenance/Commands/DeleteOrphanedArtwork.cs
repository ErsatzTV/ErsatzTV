using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Maintenance;

public record DeleteOrphanedArtwork : MediatR.IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;