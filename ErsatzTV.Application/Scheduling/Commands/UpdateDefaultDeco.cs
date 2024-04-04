namespace ErsatzTV.Application.Scheduling;

public record UpdateDefaultDeco(int PlayoutId, int? DecoId) : IRequest;
