namespace ErsatzTV.Application.Scheduling;

public record GetBlockById(int BlockId) : IRequest<Option<BlockViewModel>>;
