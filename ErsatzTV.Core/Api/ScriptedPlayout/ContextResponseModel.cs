namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContextResponseModel(
    DateTimeOffset CurrentTime,
    DateTimeOffset StartTime,
    DateTimeOffset FinishTime,
    bool IsDone);
