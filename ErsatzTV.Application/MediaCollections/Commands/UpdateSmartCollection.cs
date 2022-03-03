using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateSmartCollection(int Id, string Query) : IRequest<Either<BaseError, Unit>>;