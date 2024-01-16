using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record BlockViewModel(int Id, string Name, int Minutes, BlockStopScheduling StopScheduling);
