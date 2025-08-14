using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record BlockViewModel(int Id, int GroupId, string GroupName, string Name, int Minutes, BlockStopScheduling StopScheduling);
