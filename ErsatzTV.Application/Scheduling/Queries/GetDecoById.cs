namespace ErsatzTV.Application.Scheduling;

public record GetDecoById(int DecoId) : IRequest<Option<DecoViewModel>>;
