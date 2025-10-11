namespace ErsatzTV.Application.Scheduling;

public record GetAllBlocksForPlayout(int PlayoutId) : IRequest<List<BlockViewModel>>;
