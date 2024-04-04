using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteDecoGroup(int DecoGroupId) : IRequest<Option<BaseError>>;
