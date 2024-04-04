namespace ErsatzTV.Application.Scheduling;

public record GetDecosByDecoGroupId(int DecoGroupId) : IRequest<List<DecoViewModel>>;
