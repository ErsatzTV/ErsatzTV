using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinConnectionParameters : IRequest<Either<BaseError, JellyfinConnectionParametersViewModel>>;