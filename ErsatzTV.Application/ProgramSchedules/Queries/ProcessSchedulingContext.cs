namespace ErsatzTV.Application.ProgramSchedules;

public record ProcessSchedulingContext(string SerializedContext) : IRequest<Option<string>>;
