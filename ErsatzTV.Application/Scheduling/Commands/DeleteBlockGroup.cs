using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteBlockGroup(int BlockGroupId) : IRequest<Option<BaseError>>;
