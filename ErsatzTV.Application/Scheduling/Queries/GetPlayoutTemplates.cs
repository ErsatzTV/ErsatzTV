namespace ErsatzTV.Application.Scheduling;

public record GetPlayoutTemplates(int PlayoutId) : IRequest<List<PlayoutTemplateViewModel>>;
