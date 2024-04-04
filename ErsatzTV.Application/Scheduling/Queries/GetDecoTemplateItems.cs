namespace ErsatzTV.Application.Scheduling;

public record GetDecoTemplateItems(int DecoTemplateId) : IRequest<List<DecoTemplateItemViewModel>>;
