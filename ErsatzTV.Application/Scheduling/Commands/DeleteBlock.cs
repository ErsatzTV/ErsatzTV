using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteBlock(int BlockId) : IRequest<Option<BaseError>>;
