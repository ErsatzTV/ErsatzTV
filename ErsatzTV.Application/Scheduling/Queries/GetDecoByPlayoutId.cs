namespace ErsatzTV.Application.Scheduling;

public record GetDecoByPlayoutId(int PlayoutId) : IRequest<Option<DecoViewModel>>;
