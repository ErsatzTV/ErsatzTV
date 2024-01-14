namespace ErsatzTV.Application.Scheduling;

public record GetBlockItems(int BlockId) : IRequest<List<BlockItemViewModel>>;
