namespace ErsatzTV.Application.Scheduling;

public record GetDecoTemplatesByDecoTemplateGroupId(int DecoTemplateGroupId) : IRequest<List<DecoTemplateViewModel>>;
