namespace ErsatzTV.Application.Scheduling;

public record GetDecoTemplateById(int DecoTemplateId) : IRequest<Option<DecoTemplateViewModel>>;
