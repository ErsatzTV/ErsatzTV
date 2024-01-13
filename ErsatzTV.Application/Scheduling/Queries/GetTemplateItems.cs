namespace ErsatzTV.Application.Scheduling;

public record GetTemplateItems(int TemplateId) : IRequest<List<TemplateItemViewModel>>;
