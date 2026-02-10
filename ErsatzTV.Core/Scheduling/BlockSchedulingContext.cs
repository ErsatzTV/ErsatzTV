namespace ErsatzTV.Core.Scheduling;

public record BlockSchedulingContext(
    int BlockId,
    int BlockItemId,
    string Enumerator,
    int Seed,
    int Index);
