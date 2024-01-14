namespace ErsatzTV.Application.Scheduling;

public record GetTemplatesByTemplateGroupId(int TemplateGroupId) : IRequest<List<TemplateViewModel>>;
