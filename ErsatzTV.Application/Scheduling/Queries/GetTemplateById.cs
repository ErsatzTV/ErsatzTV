namespace ErsatzTV.Application.Scheduling;

public record GetTemplateById(int TemplateId) : IRequest<Option<TemplateViewModel>>;
