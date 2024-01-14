namespace ErsatzTV.Application.Scheduling;

public record GetBlocksByBlockGroupId(int BlockGroupId) : IRequest<List<BlockViewModel>>;
